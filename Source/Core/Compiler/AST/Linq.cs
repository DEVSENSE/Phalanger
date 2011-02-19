/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST.Linq
{
	/// <summary>
	/// LINQ ordering specifier.
	/// </summary>
	public enum Ordering
	{
		/// <summary>
		/// Ascending order.
		/// </summary>
		Ascending,

		/// <summary>
		/// Descending order.
		/// </summary>
		Descending,

		/// <summary>
		/// Default order (ascending).
		/// </summary>
		Default = Ascending
	}

	#region Expression, Query

	/// <summary>
	/// LINQ expression.
	/// </summary>
	public sealed class LinqExpression : Expression
	{
		internal override Operations Operation { get { return Operations.Linq; } }

		private QueryBody/*!*/ body;
        public QueryBody/*!*/ Body{get{return body;}}

		public LinqExpression(Position p, FromClause/*!*/ from, QueryBody/*!*/ body)
			: base(p)
		{
			Debug.Assert(from != null && body != null);

			body.FromWhere.Insert(0, from);
			this.body = body;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsLocalsWorker);
			body.Analyze(analyzer);

			return new Evaluation(this);
		}

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return false;
		}

		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			LinqBuilder builder = new LinqBuilder(codeGenerator);

			builder.DefineContextType();
			builder.EmitNewLinqContext();

			codeGenerator.LinqBuilder = builder;

			LinqOpChain chain = body.BuildChain();
			var typecode = chain.Emit(codeGenerator);

			// the result is IEnumerable<object>, let's wrap it and pass out
            il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject);

			builder.BakeContextType();

			return PhpTypeCode.Object;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLinqExpression(this);
        }
	}

	/// <summary>
	/// LINQ query body.
	/// </summary>
	public sealed class QueryBody : AstNode
	{
		internal List<FromWhereClause>/*!*/ FromWhere { get { return fromWhere; } }
		private List<FromWhereClause>/*!*/ fromWhere;

		private OrderByClause orderBy;
		private SelectClause select;
		private GroupByClause groupBy;
		private IntoClause into;

		//^invariant select != null ^ groupBy != null;

		public QueryBody(List<FromWhereClause>/*!*/ fromWhere, OrderByClause orderBy, object/*!*/ selectGroupBy, IntoClause into)
		{
			Debug.Assert(fromWhere != null && (selectGroupBy is SelectClause || selectGroupBy is GroupByClause));

			this.fromWhere = fromWhere;
			this.orderBy = orderBy;
			this.select = selectGroupBy as SelectClause;
			this.groupBy = selectGroupBy as GroupByClause;
			this.into = into;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			// check access - read only

			foreach (FromWhereClause fwc in fromWhere)
				fwc.Analyze(analyzer);

			if (orderBy != null) orderBy.Analyze(analyzer);
			if (select != null) select.Analyze(analyzer);
			if (groupBy != null) groupBy.Analyze(analyzer);
			if (into != null) into.Analyze(analyzer);
		}

		#region Transformation to Chain

		internal LinqOpChain BuildChain()
		{
			// PATTERN (Rule#1):
			//	q1 into x1 (q2 into x2 ( ... (qm into xm q)...)) 
			//
			// TRANSLATION:
			//	from x1 in ( q1 ), x2 in ( q2 ), ...,  xm in ( qm ) 
			//  q

			List<Generator> generators = new List<Generator>();

			QueryBody query = this;
			while (query.into != null)
			{
				LinqOpChain subquery_chain = query.BuildSingleQueryChain(new List<Generator>());

				generators.Add(new Generator(Position.Invalid, subquery_chain, query.into.KeyVar, query.into.ValueVar));
				query = query.into.NextQuery;
			}

			return query.BuildSingleQueryChain(generators);
		}

		private void FromWhereToGenerators(List<Generator>/*!*/ generators)
		{
			// query is the inner-most query - it will be converted to OpChain //

			int i = 0;
			for (; ; )
			{
				// convert from clauses to generators if there are any;
				// (the query can start with where-clause if there was an into-clause):
				while (i < fromWhere.Count && fromWhere[i].IsFromClause)
				{
					FromClause f = (FromClause)fromWhere[i];

					// each from-clause contains at least one generator:
					Debug.Assert(f.Generators.Count > 0);

					// adds generators contained in the from clause (Rule #2):
					foreach (Generator generator in f.Generators)
						generators.Add(generator);

					i++;
				}

				// no more clauses:
				if (i == fromWhere.Count) break;

				// where-clause follows; at least one generator has been added:
				Debug.Assert(fromWhere[i].IsWhereClause && generators.Count > 0);

				Generator last_generator = generators[generators.Count - 1];

				DirectVarUse x_key = last_generator.KeyVar;
				DirectVarUse x_value = last_generator.ValueVar;

				LinqOpChain chain = null;
				LinqOp last_op = null;

				// embeds where-clauses imediately preceded by a from-clause to 
				// the last generator of the from-clause (Rule #3):
				do
				{
					LinqOp.Where where_op = new LinqOp.Where(x_key, x_value, ((WhereClause)fromWhere[i]).Expression);

					if (last_op == null)
						chain = new LinqOpChain(last_generator.Expression, where_op);
					else
						last_op.Next = where_op;

					last_op = where_op;
					i++;
				}
				while (i < fromWhere.Count && fromWhere[i].IsWhereClause);

				Debug.Assert(chain != null);
				last_generator.Expression = chain;

				// no more clauses:
				if (i == fromWhere.Count) break;
			}
		}

		private LinqOpChain BuildSingleQueryChain(List<Generator>/*!*/ generators)
		{
			// Applies Rules #2 and #3
			FromWhereToGenerators(generators);

			if (generators.Count > 1)
			{
				// multiple from-clauses (Rules #4 to #7) //

				Expression v = (select != null) ? select.Expression : groupBy.GroupExpr;

				if (orderBy != null)
				{
					// PATTERNs (Rules #4, #5): 
					//	from x1 in e1 from x2 in e2 ... from xk in ek orderby k1, k2, ..., kn [select v | group v by g]
					//
					// TRANSLATIONs: 
					//  ( e1 ) . SelectMany ( x1 => 
					//		( e2 ) . SelectMany ( x2 =>
					//			...
					//				RESPECTIVELY:
					//				( ek ) . Select ( xk => new Tuple(k1, k2, ..., kn, v) )       
					//				( ek ) . GroupBy ( xk => g, xk => new Tuple(k1, k2, ..., kn, v) )
					//			... 
					//		)
					//  )		
					//  . OrderBy ( tuple => tuple[0] ) . ... . ThenBy ( tuple => tuple[n - 1] ) . Select ( tuple => tuple[n] )

					List<OrderingClause> clauses = orderBy.OrderingClauses;

					LinqTuple tuple = new LinqTuple(clauses, v);
					LinqOp order_by_ops = BuildTupleOrderingOps(clauses);
					return BuildSelectManyChain(generators, groupBy, tuple, order_by_ops);
				}
				else
				{
					// PATTERNs (Rules #6, #7): 
					//	from x1 in e1 from x2 in e3 ... from xk in ek [select v | group v by g]
					//
					// TRANSLATIONs: 
					//  ( e1 ) . SelectMany ( x1 => 
					//		( e2 ) . SelectMany ( x2 =>
					//			...
					//				RESPECTIVELY:
					//				( ek ) . Select ( xk => v )       
					//				( ek ) . GroupBy ( xk => g, xk => v )
					//			... 
					//		)
					//  ) 

					return BuildSelectManyChain(generators, groupBy, v, null);
				}
			}
			else
			{
				// single from-clause (Rules #8 to #11) //

				DirectVarUse x_key = generators[0].KeyVar;
				DirectVarUse x_value = generators[0].ValueVar;

				LinqOp last_op;
				if (select != null)
					last_op = new LinqOp.Select(x_key, x_value, select.Expression);
				else
					last_op = new LinqOp.GroupBy(x_key, x_value, groupBy.GroupExpr, groupBy.ByExpr);

				if (orderBy != null)
				{
					// PATTERNs (Rules #10, #11): 
					//	from x in e orderby k1 , k2, ..., kn [select v | group v by g]
					//
					// TRANSLATIONs:
					//	( e ) . OrderBy ( x => k1 ) . ThenBy ( x => k2 ) ... . ThenBy (x => kn) . Select ( x => v )
					//	( e ) . OrderBy ( x => k1 ) . ThenBy ( x => k2 ) ... . ThenBy (x => kn) . GroupBy ( x => g, x => v )

					return new LinqOpChain(
						generators[0].Expression,
						BuildOrderingOps(x_key, x_value, orderBy.OrderingClauses, last_op)
					);
				}
				else
				{
					// PATTERNs (Rules #8, #9): 
					//	from x in e [select v | group v by g]
					//
					// TRANSLATIONs:
					//	( e ) . Select ( x => v )
					//  ( e ) . GroupBy ( x => g, x => v )

					return new LinqOpChain(generators[0].Expression, last_op);
				}
			}
		}

		private static LinqOpChain BuildSelectManyChain(List<Generator>/*!*/ generators,
			GroupByClause gbClause, Expression/*!*/ v, LinqOp lastOp)
		{
			// PATTERNs (Rules #6, #7): 
			//	from x1 in e1 from x2 in e3 ... from xk in ek [select v | group v by g]
			//
			// TRANSLATIONs: 
			//  ( e1 ) . SelectMany ( x1 => 
			//		( e2 ) . SelectMany ( x2 =>
			//			...
			//				RESPECTIVELY:
			//				( ek ) . Select ( xk => v )       
			//				( ek ) . GroupBy ( xk => g, xk => v )
			//			... 
			//		)
			//  ) 
			//	. [lastOp]

			int i = generators.Count - 1;

			LinqOp outer_op; // outer-most operator in the current chain

			if (gbClause != null)
				outer_op = new LinqOp.GroupBy(generators[i].KeyVar, generators[i].ValueVar, gbClause.ByExpr, v);
			else
				outer_op = new LinqOp.Select(generators[i].KeyVar, generators[i].ValueVar, v);

			// inner-most:
			LinqOpChain inner_chain = new LinqOpChain(generators[i].Expression, outer_op);

			while (--i >= 0)
			{
				inner_chain = new LinqOpChain(
					generators[i].Expression,
					outer_op = new LinqOp.SelectMany(generators[i].KeyVar, generators[i].ValueVar, inner_chain)
				);
			}

			outer_op.Next = lastOp;

			return inner_chain;
		}

		private static LinqOp BuildOrderingOps(DirectVarUse keyVar, DirectVarUse valueVar,
			List<OrderingClause>/*!*/ clauses, LinqOp lastOp)
		{
			// PATTERNs: 
			//	from x in e orderby k1 , k2, ..., kn [select v | group v by g]
			//
			// TRANSLATIONs:
			//	( e ) . OrderBy ( x => k1 ) . ThenBy ( x => k2 ) ... . ThenBy (x => kn) . Select ( x => v )
			//	( e ) . OrderBy ( x => k1 ) . ThenBy ( x => k2 ) ... . ThenBy (x => kn) . GroupBy ( x => g, x => v )

			Debug.Assert(clauses != null && clauses.Count > 0);

			for (int i = clauses.Count - 1; i >= 0; i--)
				lastOp = new LinqOp.OrderBy(keyVar, valueVar, clauses[i].Expression, clauses[i].Ordering, lastOp, i != 0);

			return lastOp;
		}

		private static LinqOp BuildTupleOrderingOps(List<OrderingClause> clauses)
		{
			// tuple of size n + 1
			// 
			// TRANSLATION:
			//  . OrderBy ( x => x[0] ) . ThenBy ( x => x[1] ) . ... . ThenBy ( x => x[n - 1] ) . Select ( x => x[n] )

			LinqOp last_op = new LinqOp.Select(null, null, new LinqTupleItemAccess(clauses.Count, clauses.Count + 1));

			for (int i = clauses.Count - 1; i >= 0; i--)
				last_op = new LinqOp.OrderBy(null, null, new LinqTupleItemAccess(i, clauses.Count + 1), clauses[i].Ordering, last_op, i != 0);

			return last_op;
		}

		#endregion
	}

	#endregion

	#region Clauses

	/// <summary>
	/// LINQ select clause.
	/// </summary>
	public sealed class SelectClause : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } set { expression = value; } }
		private Expression/*!*/ expression;

		public SelectClause(Position p, Expression/*!*/ expression)
			: base(p)
		{
			Debug.Assert(expression != null);

			this.expression = expression;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitSelectClause(this);
        }
	}

	/// <summary>
	/// LINQ group by clause.
	/// </summary>
	public sealed class GroupByClause : LangElement
	{
		public Expression/*!*/ GroupExpr { get { return groupExpr; } set { groupExpr = value; } }
		private Expression/*!*/ groupExpr;

		public Expression/*!*/ ByExpr { get { return byExpr; } set { byExpr = value; } }
		private Expression/*!*/ byExpr;

		public GroupByClause(Position position, Expression/*!*/ groupExpr, Expression/*!*/ byExpr)
			: base(position)
		{
			Debug.Assert(groupExpr != null && byExpr != null);

			this.groupExpr = groupExpr;
			this.byExpr = byExpr;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			groupExpr = groupExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			byExpr = byExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGroupByClause(this);
        }
	}

	/// <summary>
	/// LINQ into clause.
	/// </summary>
	public sealed class IntoClause : LangElement
	{
		public DirectVarUse KeyVar { get { return keyVar; } }
		private DirectVarUse keyVar;

		public DirectVarUse/*!*/ ValueVar { get { return valueVar; } }
		private DirectVarUse/*!*/ valueVar;

		public QueryBody/*!*/ NextQuery { get { return nextQuery; } }
		private QueryBody/*!*/ nextQuery;

		public IntoClause(Position p, DirectVarUse keyVar, DirectVarUse/*!*/ valueVar, QueryBody/*!*/ nextQuery)
			: base(p)
		{
			Debug.Assert(valueVar != null && nextQuery != null);

			this.keyVar = keyVar;
			this.valueVar = valueVar;
			this.nextQuery = nextQuery;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			ExInfoFromParent winfo = new ExInfoFromParent(this, AccessType.Write);
			if (keyVar != null) keyVar.Analyze(analyzer, winfo);
			valueVar.Analyze(analyzer, winfo);

			nextQuery.Analyze(analyzer);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIntoClause(this);
        }
	}

	/// <summary>
	/// LINQ order by clause.
	/// </summary>
	public sealed class OrderByClause : LangElement
	{
		public List<OrderingClause>/*!*/ OrderingClauses { get { return orderingClauses; } set { orderingClauses = value; } }
		private List<OrderingClause>/*!*/ orderingClauses;

		public OrderByClause(Position p, List<OrderingClause>/*!*/ orderingClauses)
			: base(p)
		{
			Debug.Assert(orderingClauses != null);

			this.orderingClauses = orderingClauses;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			foreach (OrderingClause clause in orderingClauses)
				clause.Analyze(analyzer);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitOrderByClause(this);
        }
	}

	/// <summary>
	/// LINQ ordering expression.
	/// </summary>
	public sealed class OrderingClause : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } set { expression = value; } }
		private Expression/*!*/ expression;

		public Ordering Ordering { get { return ordering; } }
		private Ordering ordering;

		public OrderingClause(Position p, Expression/*!*/ expression, Ordering ordering)
			: base(p)
		{
			Debug.Assert(expression != null);

			this.expression = expression;
			this.ordering = ordering;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitOrderingClause(this);
        }
	}

	/// <summary>
	/// LINQ from or where clause.
	/// </summary>
	public abstract class FromWhereClause : LangElement
	{
		internal abstract bool IsFromClause { get; }
		internal bool IsWhereClause { get { return !IsFromClause; } }

		public FromWhereClause(Position p)
			: base(p)
		{
		}

		internal abstract void Analyze(Analyzer/*!*/ analyzer);
	}

	/// <summary>
	/// LINQ where clause.
	/// </summary>
	public sealed class WhereClause : FromWhereClause
	{
		internal override bool IsFromClause { get { return false; } }

		public Expression/*!*/ Expression { get { return expression; } }
		private Expression/*!*/ expression;

		public WhereClause(Position position, Expression/*!*/ expression)
			: base(position)
		{
			Debug.Assert(expression != null);

			this.expression = expression;
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitWhereClause(this);
        }
	}

	/// <summary>
	/// LINQ from clause.
	/// </summary>
	public sealed class FromClause : FromWhereClause
	{
		internal override bool IsFromClause { get { return true; } }

		public List<Generator>/*!*/ Generators { get { return generators; } }
		private List<Generator>/*!*/ generators;

		public FromClause(Position p, List<Generator>/*!*/ generators)
			: base(p)
		{
			Debug.Assert(generators != null);
			this.generators = generators;
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			foreach (Generator g in generators)
				g.Analyze(analyzer);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFromClause(this);
        }
	}

	/// <summary>
	/// LINQ generator definition.
	/// </summary>
	public sealed class Generator : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } internal set { expression = value; } }
		private Expression/*!*/ expression;

		public DirectVarUse KeyVar { get { return keyVar; } }
		private DirectVarUse keyVar;

		public DirectVarUse/*!*/ ValueVar { get { return valueVar; } }
		private DirectVarUse/*!*/ valueVar;

		public Generator(Position p, Expression/*!*/ expression, DirectVarUse keyVar, DirectVarUse/*!*/ valueVar)
			: base(p)
		{
			Debug.Assert(expression != null && valueVar != null);

			this.expression = expression;
			this.keyVar = keyVar;
			this.valueVar = valueVar;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			ExInfoFromParent winfo = new ExInfoFromParent(this, AccessType.Write);
			if (keyVar != null) keyVar.Analyze(analyzer, winfo);
			valueVar.Analyze(analyzer, winfo);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGenerator(this);
        }
	}

	#endregion

	#region Operations

	internal abstract class LinqOp
	{
		public LinqOp Next { get { return next; } set { next = value; } }
		private LinqOp next;

		/// <summary>
		/// Emit next operation in the chain or nothing if next operation is null.
		/// To be used from inherited operations.
		/// </summary>
		public virtual PhpTypeCode Emit(LinqBuilder/*!*/ builder)
		{
			if (Next != null) return Next.Emit(builder); else return PhpTypeCode.LinqSource;
		}

		#region Where

		internal sealed class Where : LinqOp
		{
			private DirectVarUse keyVar;
			private DirectVarUse valueVar;
			private Expression predicate;

			public Where(DirectVarUse keyVar, DirectVarUse valueVar, Expression predicate)
			{
				this.keyVar = keyVar;
				this.valueVar = valueVar;
				this.predicate = predicate;
			}

			public override PhpTypeCode Emit(LinqBuilder/*!*/ builder)
			{
				ILEmitter il = builder.IL;

				// source expected on stack

				// NEW Func[object,object](<linq context>, <&lambda>);
				builder.EmitLoadLinqContext();
				il.Emit(OpCodes.Ldftn, builder.EmitLambda(string.Format("<Predicate_{0}>",
					builder.GetNextPredicateNum()), valueVar, predicate, PhpTypeCode.Boolean));
				il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_bool_ctor);

				// LOAD Where[object](<source>, <delegate>);
				il.Emit(OpCodes.Call, LinqExterns.Where);
				//il.Emit(OpCodes.Call, Methods.Operators.Where);

				return base.Emit(builder);
			}
		}

		#endregion

		#region OrderBy, ThenBy, OrderByDescending, ThenByDescending

		internal sealed class OrderBy : LinqOp
		{
			private DirectVarUse keyVar;
			private DirectVarUse valueVar;
			private Expression expression;
			private Ordering ordering;
			private bool isThenBy;

			public OrderBy(DirectVarUse keyVar, DirectVarUse valueVar, Expression expression,
				Ordering ordering, LinqOp next, bool isThenBy)
			{
				this.keyVar = keyVar;
				this.valueVar = valueVar;
				this.expression = expression;
				this.ordering = ordering;
				this.next = next;
				this.isThenBy = isThenBy;
			}

			/// <summary>
			/// Emit lambda function that returns values used for sorting and
			/// call to OrderedSequence.OrderBy method (with current LINQ context as 
			/// first and generated lambda as a second parameter)
			/// </summary>
			public override PhpTypeCode Emit(LinqBuilder codeGenerator)
			{
				ILEmitter il = codeGenerator.IL;

				// NEW Func[object,object](<linq context>, <&lambda>);
				codeGenerator.EmitLoadLinqContext();
				il.Emit(OpCodes.Ldftn, codeGenerator.EmitLambda(string.Format("<Comparer_{0}>",
					codeGenerator.GetNextComparerNum()), valueVar, expression, PhpTypeCode.Object));
				il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_object_ctor);

				// LOAD Select[object,object](<source>, <delegate>);
				if (isThenBy)
					il.Emit(OpCodes.Call, ordering == Ordering.Descending ? LinqExterns.ThenByDescending : LinqExterns.ThenBy);
				else
					il.Emit(OpCodes.Call, ordering == Ordering.Descending ? LinqExterns.OrderByDescending : LinqExterns.OrderBy);

				return base.Emit(codeGenerator);
			}
		}

		#endregion

		#region Select

		internal sealed class Select : LinqOp
		{
			private DirectVarUse keyVar;
			private DirectVarUse valueVar;
			private Expression selector;

			public Select(DirectVarUse keyVar, DirectVarUse valueVar, SelectClause clause)
				: this(keyVar, valueVar, clause.Expression)
			{
			}

			public Select(DirectVarUse keyVar, DirectVarUse valueVar, Expression selector)
			{
				this.keyVar = keyVar;
				this.valueVar = valueVar;
				this.selector = selector;
			}


			/// <summary>
			/// Emit lambda function that implements projection and call to
			/// IEnumerable.Select function (with current LINQ context as 
			/// first and generated lambda as a second parameter)
			/// </summary>
			public override PhpTypeCode Emit(LinqBuilder/*!*/ builder)
			{
				ILEmitter il = builder.IL;

				// source expected on stack

				// NEW Func[object,object](<linq context>, <&lambda>);
				builder.EmitLoadLinqContext();
				il.Emit(OpCodes.Ldftn, builder.EmitLambda(string.Format("<Selector_{0}>",
					builder.GetNextSelectorNum()), valueVar, selector, PhpTypeCode.Object));
				il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_object_ctor);

				// LOAD Select[object,object](<source>, <delegate>);
				il.Emit(OpCodes.Call, LinqExterns.Select);
				//il.Emit(OpCodes.Call, Methods.Operators.Select);

				return base.Emit(builder);
			}

		}

		#endregion

		#region SelectMany

		internal sealed class SelectMany : LinqOp
		{
			private DirectVarUse keyVar;
			private DirectVarUse valueVar;
			private LinqOpChain innerChain;

			public SelectMany(DirectVarUse keyVar, DirectVarUse valueVar, LinqOpChain innerChain)
			{
				this.keyVar = keyVar;
				this.valueVar = valueVar;
				this.innerChain = innerChain;
			}

			public override PhpTypeCode Emit(LinqBuilder builder)
			{
				ILEmitter il = builder.IL;

				// source expected on stack

				// NEW Func[object,object](<linq context>, <&lambda>);
				builder.EmitLoadLinqContext();
				il.Emit(OpCodes.Ldftn, builder.EmitLambda(string.Format("<MultiSelector_{0}>",
					builder.GetNextMultiSelectorNum()), valueVar, innerChain, PhpTypeCode.LinqSource));
				il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_IEnumerable_object_ctor);

				// LOAD Select[object,object](<source>, <delegate>);
				il.Emit(OpCodes.Call, LinqExterns.SelectMany);

				return base.Emit(builder);
			}
		}

		#endregion

		#region GroupBy

		internal sealed class GroupBy : LinqOp
		{
			private DirectVarUse keyVar;
			private DirectVarUse valueVar;
			private Expression groupExpr;
			private Expression byExpr;

			public GroupBy(DirectVarUse keyVar, DirectVarUse valueVar, GroupByClause clause)
				: this(keyVar, valueVar, clause.GroupExpr, clause.ByExpr)
			{
			}

			public GroupBy(DirectVarUse keyVar, DirectVarUse valueVar, Expression groupExpr, Expression byExpr)
			{
				this.keyVar = keyVar;
				this.valueVar = valueVar;
				this.groupExpr = groupExpr;
				this.byExpr = byExpr;
			}

			public override PhpTypeCode Emit(LinqBuilder codeGenerator)
			{
				ILEmitter il = codeGenerator.IL;

				// NEW Func[object,object](<linq context>, <&lambda>);
				codeGenerator.EmitLoadLinqContext();
				il.Emit(OpCodes.Ldftn, codeGenerator.EmitLambda(string.Format("<Selector_{0}>",
					codeGenerator.GetNextSelectorNum()), valueVar, byExpr, PhpTypeCode.Object));
				il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_object_ctor);

				DirectVarUse groupByVar = groupExpr as DirectVarUse;
				if ((groupByVar != null) && (groupByVar.VarName == valueVar.VarName))
				{
					// LOAD Select[object,object](<source>, <delegate>);
					// Simplified version - no element selector
					il.Emit(OpCodes.Call, LinqExterns.GroupBy);
				}
				else
				{
					// with element selector
					codeGenerator.EmitLoadLinqContext();
					il.Emit(OpCodes.Ldftn, codeGenerator.EmitLambda(string.Format("<Selector_{0}>",
						codeGenerator.GetNextSelectorNum()), valueVar, groupExpr, PhpTypeCode.Object));
					il.Emit(OpCodes.Newobj, LinqExterns.Func2_object_object_ctor);

					il.Emit(OpCodes.Call, LinqExterns.GroupByElementSel);
				}

				// Conversion from IEnumerable<IGrouping<..>> to IEnumerable<object> 
				il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject); 
				codeGenerator.CodeGenerator.EmitLoadClassContext();
				il.Emit(OpCodes.Call, Methods.Convert.ObjectToLinqSource);
				return base.Emit(codeGenerator);
			}
		}

		#endregion
	}

	#endregion

	#region Synthetic Expressions

	internal sealed class LinqOpChain : Expression
	{
		internal override Operations Operation { get { return Operations.LinqOpChain; } }

		public Expression/*!*/ Expression { get { return expression; } set { expression = value; } }
		private Expression/*!*/ expression;

		public LinqOp First { get { return firstOp; } set { firstOp = value; } }
		private LinqOp firstOp;

		public LinqOpChain(Expression/*!*/ expression, LinqOp firstOp)
			: base(Position.Invalid)
		{
			this.expression = expression;
			this.firstOp = firstOp;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			return new Evaluation(this);
		}

		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			LinqBuilder builder = codeGenerator.LinqBuilder;
			ILEmitter il = builder.IL;

			codeGenerator.EmitConversion(expression, PhpTypeCode.LinqSource);
			firstOp.Emit(builder);

			return PhpTypeCode.LinqSource;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLinqOpChain(this);
        }
	}

	internal sealed class LinqTuple : Expression
	{
		internal override Operations Operation { get { return Operations.LinqTuple; } }

		public Expression LastItem { get { return lastItem; } set { lastItem = value; } }
		private Expression lastItem;

		public List<OrderingClause> OrderingItems { get { return orderingItems; } set { orderingItems = value; } }
		private List<OrderingClause> orderingItems;

		public LinqTuple(List<OrderingClause> orderingItems, Expression lastItem)
			: base(Position.Invalid)
		{
			this.orderingItems = orderingItems;
			this.lastItem = lastItem;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			throw new InvalidOperationException();
		}

		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			// emit tuple value
			foreach (OrderingClause clause in OrderingItems)
				clause.Expression.Emit(codeGenerator);
			LastItem.Emit(codeGenerator);

			// emit tuple creation
			ILEmitter il = codeGenerator.IL;
			for (int indirection = 0; indirection < OrderingItems.Count; indirection++)
			{
				il.Emit(OpCodes.Newobj, LinqExterns.GetTupleInfo(indirection).Constructor);
			}
			return PhpTypeCode.Object;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLinqTuple(this);
        }
	}

	internal sealed class LinqTupleItemAccess : Expression
	{
		internal override Operations Operation { get { return Operations.LinqTupleItemAccess; } }

		public int Index { get { return index; } set { index = value; } }
		private int index;

		public int Count { get { return count; } }
		private int count;

		public LinqTupleItemAccess(int index, int count)
			: base(Position.Invalid)
		{
			Debug.Assert(count >= 2);

			this.index = index;
			this.count = count;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			throw new InvalidOperationException();
		}

		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			int indir = this.count - 2;

			// cast object to correct tuple type:
			//   Tuple<Tuple<...Tuple<object, object>, .. >, object>, object>
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Castclass, LinqExterns.GetTupleInfo(indir).Type);

			// Find value at specified index (tup.First.First....First/Second);
			int timesFirst = index - ((index == count - 1) ? 1 : 0);
			for (int i = 0; i < timesFirst; i++)
			{
				il.Emit(OpCodes.Call, LinqExterns.GetTupleInfo(indir - i).SecondGetter);
			}
			
			// The last value is stored in Second, other values are stored in First
			il.Emit(OpCodes.Call, (index == count - 1)?
				LinqExterns.GetTupleInfo(indir - timesFirst).SecondGetter:
				LinqExterns.GetTupleInfo(indir - timesFirst).FirstGetter);
			return PhpTypeCode.Object;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLinqTupleItemAccess(this);
        }
	}

	#endregion
}