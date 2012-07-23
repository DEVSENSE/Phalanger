using System;
using System.Collections;
using System.Collections.Generic;
namespace Lex
{
	public class Minimize
	{
		private Spec spec;
		private List<List<DTrans>> group;
		private int[] ingroup;
		public Minimize()
		{
			this.reset();
		}
		private void reset()
		{
			this.spec = null;
			this.group = null;
			this.ingroup = null;
		}
		private void set(Spec s)
		{
			this.spec = s;
			this.group = null;
			this.ingroup = null;
		}
		public void min_dfa(Spec s)
		{
			this.set(s);
			this.minimize();
			this.reduce();
			this.reset();
		}
		private void col_copy(int dest, int src)
		{
			int count = this.spec.dtrans_list.Count;
			for (int i = 0; i < count; i++)
			{
				DTrans dTrans = this.spec.dtrans_list[i];
				dTrans.SetDTrans(dest, dTrans.GetDTrans(src));
			}
		}
		private void row_copy(int dest, int src)
		{
			this.spec.dtrans_list[dest] = this.spec.dtrans_list[src];
		}
		private bool col_equiv(int col1, int col2)
		{
			int count = this.spec.dtrans_list.Count;
			for (int i = 0; i < count; i++)
			{
				DTrans dTrans = this.spec.dtrans_list[i];
				if (dTrans.GetDTrans(col1) != dTrans.GetDTrans(col2))
				{
					return false;
				}
			}
			return true;
		}
		private bool row_equiv(int row1, int row2)
		{
			DTrans dTrans = this.spec.dtrans_list[row1];
			DTrans dTrans2 = this.spec.dtrans_list[row2];
			for (int i = 0; i < this.spec.dtrans_ncols; i++)
			{
				if (dTrans.GetDTrans(i) != dTrans2.GetDTrans(i))
				{
					return false;
				}
			}
			return true;
		}
		private void reduce()
		{
			int count = this.spec.dtrans_list.Count;
			BitArray bitArray = new BitArray(count);
			this.spec.anchor_array = new int[count];
			this.spec.accept_list = new List<Accept>();
			for (int i = 0; i < count; i++)
			{
				DTrans dTrans = this.spec.dtrans_list[i];
				this.spec.accept_list.Add(dTrans.GetAccept());
				this.spec.anchor_array[i] = dTrans.GetAnchor();
				dTrans.SetAccept(null);
			}
			this.spec.col_map = new int[this.spec.dtrans_ncols];
			for (int i = 0; i < this.spec.dtrans_ncols; i++)
			{
				this.spec.col_map[i] = -1;
			}
			int num = 0;
			while (true)
			{
				int i;
				for (i = 0; i < num; i++)
				{
				}
				i = num;
				while (i < this.spec.dtrans_ncols && -1 != this.spec.col_map[i])
				{
					i++;
				}
				if (i >= this.spec.dtrans_ncols)
				{
					break;
				}
				if (i >= bitArray.Length)
				{
					bitArray.Length = i + 1;
				}
				bitArray.Set(i, true);
				this.spec.col_map[i] = num;
				for (int j = i + 1; j < this.spec.dtrans_ncols; j++)
				{
					if (-1 == this.spec.col_map[j] && this.col_equiv(i, j))
					{
						this.spec.col_map[j] = num;
					}
				}
				num++;
			}
			int num2 = 0;
			for (int i = 0; i < this.spec.dtrans_ncols; i++)
			{
				if (i >= bitArray.Length)
				{
					bitArray.Length = i + 1;
				}
				if (bitArray.Get(i))
				{
					num2++;
					bitArray.Set(i, false);
					int j = this.spec.col_map[i];
					if (j != i)
					{
						this.col_copy(j, i);
					}
				}
			}
			this.spec.dtrans_ncols = num;
			int count2 = this.spec.dtrans_list.Count;
			this.spec.row_map = new int[count2];
			for (int i = 0; i < count2; i++)
			{
				this.spec.row_map[i] = -1;
			}
			int num3 = 0;
			while (true)
			{
				int i;
				for (i = 0; i < num3; i++)
				{
				}
				i = num3;
				while (i < count2 && -1 != this.spec.row_map[i])
				{
					i++;
				}
				if (i >= count2)
				{
					break;
				}
				bitArray.Set(i, true);
				this.spec.row_map[i] = num3;
				for (int j = i + 1; j < count2; j++)
				{
					if (-1 == this.spec.row_map[j] && this.row_equiv(i, j))
					{
						this.spec.row_map[j] = num3;
					}
				}
				num3++;
			}
			num2 = 0;
			for (int i = 0; i < count2; i++)
			{
				if (bitArray.Get(i))
				{
					num2++;
					bitArray.Set(i, false);
					int j = this.spec.row_map[i];
					if (j != i)
					{
						this.row_copy(j, i);
					}
				}
			}
			this.spec.dtrans_list.RemoveRange(num3, count - num3);
		}
		private void fix_dtrans()
		{
			List<DTrans> list = new List<DTrans>();
			int num = this.spec.state_dtrans.Length;
			for (int i = 0; i < num; i++)
			{
				if (-1 != this.spec.state_dtrans[i])
				{
					this.spec.state_dtrans[i] = this.ingroup[this.spec.state_dtrans[i]];
				}
			}
			num = this.group.Count;
			for (int i = 0; i < num; i++)
			{
				List<DTrans> list2 = this.group[i];
				DTrans dTrans = list2[0];
				list.Add(dTrans);
				for (int j = 0; j < this.spec.dtrans_ncols; j++)
				{
					if (-1 != dTrans.GetDTrans(j))
					{
						dTrans.SetDTrans(j, this.ingroup[dTrans.GetDTrans(j)]);
					}
				}
			}
			this.group = null;
			this.spec.dtrans_list = list;
		}
		private void minimize()
		{
			this.init_groups();
			int num = this.group.Count;
			int num2 = num - 1;
			while (num2 != num)
			{
				num2 = num;
				for (int i = 0; i < num; i++)
				{
					List<DTrans> list = this.group[i];
					int num3 = list.Count;
					if (num3 > 1)
					{
						List<DTrans> list2 = new List<DTrans>();
						bool flag = false;
						DTrans dTrans = list[0];
						for (int j = 1; j < num3; j++)
						{
							DTrans dTrans2 = list[j];
							for (int k = 0; k < this.spec.dtrans_ncols; k++)
							{
								int dTrans3 = dTrans.GetDTrans(k);
								int dTrans4 = dTrans2.GetDTrans(k);
								if (dTrans3 != dTrans4 && (dTrans3 == -1 || dTrans4 == -1 || this.ingroup[dTrans4] != this.ingroup[dTrans3]))
								{
									list.RemoveAt(j);
									j--;
									num3--;
									list2.Add(dTrans2);
									if (!flag)
									{
										flag = true;
										num++;
										this.group.Add(list2);
									}
									this.ingroup[dTrans2.Label] = this.group.Count - 1;
									break;
								}
							}
						}
					}
				}
			}
			Console.WriteLine(this.group.Count + " states after removal of redundant states.");
			this.fix_dtrans();
		}
		private void init_groups()
		{
			int num = 0;
			this.group = new List<List<DTrans>>();
			int count = this.spec.dtrans_list.Count;
			this.ingroup = new int[count];
			for (int i = 0; i < count; i++)
			{
				bool flag = false;
				DTrans dTrans = this.spec.dtrans_list[i];
				for (int j = 0; j < num; j++)
				{
					List<DTrans> list = this.group[j];
					DTrans dTrans2 = list[0];
					for (int k = 1; k < list.Count; k++)
					{
					}
					if (dTrans2.GetAccept() == dTrans.GetAccept())
					{
						list.Add(dTrans);
						this.ingroup[i] = j;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					List<DTrans> list2 = new List<DTrans>();
					list2.Add(dTrans);
					this.ingroup[i] = this.group.Count;
					this.group.Add(list2);
					num++;
				}
			}
		}
		private void pset(List<DTrans> dtrans_group)
		{
			int count = dtrans_group.Count;
			for (int i = 0; i < count; i++)
			{
				DTrans dTrans = dtrans_group[i];
				Console.Write(dTrans.Label + " ");
			}
		}
		private void pgroups()
		{
			int count = this.group.Count;
			for (int i = 0; i < count; i++)
			{
				Console.Write("\tGroup " + i + " {");
				this.pset(this.group[i]);
				Console.WriteLine("}\n");
			}
			Console.WriteLine("");
			int count2 = this.spec.dtrans_list.Count;
			for (int j = 0; j < count2; j++)
			{
				Console.WriteLine(string.Concat(new object[]
				{
					"\tstate ",
					j,
					" is in group ",
					this.ingroup[j]
				}));
			}
		}
	}
}
