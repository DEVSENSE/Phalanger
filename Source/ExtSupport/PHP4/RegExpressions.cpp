//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// RegExpressions.cpp
// - contains definitions of regular expression related exported functions
//

#include "stdafx.h"

#include <sys/types.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <limits.h>
#include <stdlib.h>
#include <assert.h>

#include "RegExpressions.h"

//
// regerror.c:
//

//#include "utils.h"
//#include "regerror.ih"

/*
 = #define	REG_OKAY	 0
 = #define	REG_NOMATCH	 1
 = #define	REG_BADPAT	 2
 = #define	REG_ECOLLATE	 3
 = #define	REG_ECTYPE	 4
 = #define	REG_EESCAPE	 5
 = #define	REG_ESUBREG	 6
 = #define	REG_EBRACK	 7
 = #define	REG_EPAREN	 8
 = #define	REG_EBRACE	 9
 = #define	REG_BADBR	10
 = #define	REG_ERANGE	11
 = #define	REG_ESPACE	12
 = #define	REG_BADRPT	13
 = #define	REG_EMPTY	14
 = #define	REG_ASSERT	15
 = #define	REG_INVARG	16
 = #define	REG_ATOI	255	// convert name to number (!)
 = #define	REG_ITOA	0400	// convert number to name (!)
 */

#pragma unmanaged

static struct rerr {
	int code;
	char *name;
	char *explain;
} rerrs[] = {
	{REG_OKAY,	"REG_OKAY",	"no errors detected"},
	{REG_NOMATCH,	"REG_NOMATCH",	"regexec() failed to match"},
	{REG_BADPAT,	"REG_BADPAT",	"invalid regular expression"},
	{REG_ECOLLATE,	"REG_ECOLLATE",	"invalid collating element"},
	{REG_ECTYPE,	"REG_ECTYPE",	"invalid character class"},
	{REG_EESCAPE,	"REG_EESCAPE",	"trailing backslash (\\)"},
	{REG_ESUBREG,	"REG_ESUBREG",	"invalid backreference number"},
	{REG_EBRACK,	"REG_EBRACK",	"brackets ([ ]) not balanced"},
	{REG_EPAREN,	"REG_EPAREN",	"parentheses not balanced"},
	{REG_EBRACE,	"REG_EBRACE",	"braces not balanced"},
	{REG_BADBR,	"REG_BADBR",	"invalid repetition count(s)"},
	{REG_ERANGE,	"REG_ERANGE",	"invalid character range"},
	{REG_ESPACE,	"REG_ESPACE",	"out of memory"},
	{REG_BADRPT,	"REG_BADRPT",	"repetition-operator operand invalid"},
	{REG_EMPTY,	"REG_EMPTY",	"empty (sub)expression"},
	{REG_ASSERT,	"REG_ASSERT",	"\"can't happen\" -- you found a bug"},
	{REG_INVARG,	"REG_INVARG",	"invalid argument to regex routine"},
	{-1,		"",		"*** unknown regexp error code ***"},
};

/*
 - regatoi - internal routine to implement REG_ATOI
 == static char *regatoi(const regex_t *preg, char *localbuf);
 */
static char *
regatoi(const regex_t *preg, char *localbuf)
{
	register struct rerr *r;

	for (r = rerrs; r->code >= 0; r++)
		if (strcmp(r->name, preg->re_endp) == 0)
			break;
	if (r->code < 0)
		return("0");

	sprintf(localbuf, "%d", r->code);
	return(localbuf);
}

/*
 - regerror - the interface to error numbers
 = API_EXPORT(size_t) regerror(int, const regex_t *, char *, size_t);
 */
/* ARGSUSED */
API_EXPORT(size_t)
regerror(int errcode, const regex_t *preg, char *errbuf, size_t errbuf_size)
{
	register struct rerr *r;
	register size_t len;
	register int target = errcode &~ REG_ITOA;
	register char *s;
	char convbuf[50];

	if (errcode == REG_ATOI)
		s = regatoi(preg, convbuf);
	else {
		for (r = rerrs; r->code >= 0; r++)
			if (r->code == target)
				break;
	
		if (errcode&REG_ITOA) {
			if (r->code >= 0)
				(void) strcpy(convbuf, r->name);
			else
				sprintf(convbuf, "REG_0x%x", target);
			assert(strlen(convbuf) < sizeof(convbuf));
			s = convbuf;
		} else
			s = r->explain;
	}

	len = strlen(s) + 1;
	if (errbuf_size > 0) {
		if (errbuf_size > len)
			(void) strcpy(errbuf, s);
		else {
			(void) strncpy(errbuf, s, errbuf_size-1);
			errbuf[errbuf_size-1] = '\0';
		}
	}

	return(len);
}

//
// regcomp.c:
//

#define POSIX_MISTAKE

//#include "cclass.h"
//#include "cname.h"

/*
 * parse structure, passed up and down to avoid global variables and
 * other clumsinesses
 */
struct parse {
	char *next;		/* next character in RE */
	char *end;		/* end of string (-> NUL normally) */
	int error;		/* has an error been seen? */
	sop *strip;		/* malloced strip */
	sopno ssize;		/* malloced strip size (allocated) */
	sopno slen;		/* malloced strip length (used) */
	int ncsalloc;		/* number of csets allocated */
	struct re_guts *g;
#	define	NPAREN	10	/* we need to remember () 1-9 for back refs */
	sopno pbegin[NPAREN];	/* -> ( ([0] unused) */
	sopno pend[NPAREN];	/* -> ) ([0] unused) */
};

//#include "regcomp.ih"

static char nuls[10];		/* place to point scanner in event of error */

/*
 * macros for use with parse structure
 * BEWARE:  these know that the parse structure is named `p' !!!
 */
#define	PEEK()	(*p->next)
#define	PEEK2()	(*(p->next+1))
#define	MORE()	(p->next < p->end)
#define	MORE2()	(p->next+1 < p->end)
#define	SEE(c)	(MORE() && PEEK() == (c))
#define	SEETWO(a, b)	(MORE() && MORE2() && PEEK() == (a) && PEEK2() == (b))
#define	EAT(c)	((SEE(c)) ? (NEXT(), 1) : 0)
#define	EATTWO(a, b)	((SEETWO(a, b)) ? (NEXT2(), 1) : 0)
#define	NEXT()	(p->next++)
#define	NEXT2()	(p->next += 2)
#define	NEXTn(n)	(p->next += (n))
#define	GETNEXT()	(*p->next++)
#define	SETERROR(e)	seterr(p, (e))
#define	REQUIRE(co, e)	(void) ((co) || SETERROR(e))
#define	MUSTSEE(c, e)	(REQUIRE(MORE() && PEEK() == (c), e))
#define	MUSTEAT(c, e)	(REQUIRE(MORE() && GETNEXT() == (c), e))
#define	MUSTNOTSEE(c, e)	(REQUIRE(!MORE() || PEEK() != (c), e))
#define	EMIT(op, sopnd)	doemit(p, (sop)(op), (size_t)(sopnd))
#define	INSERT(op, pos)	doinsert(p, (sop)(op), HERE()-(pos)+1, pos)
#define	AHEAD(pos)		dofwd(p, pos, HERE()-(pos))
#define	ASTERN(sop, pos)	EMIT(sop, HERE()-pos)
#define	HERE()		(p->slen)
#define	THERE()		(p->slen - 1)
#define	THERETHERE()	(p->slen - 2)
#define	DROP(n)	(p->slen -= (n))

#ifndef NDEBUG
static int never = 0;		/* for use in asserts; shuts lint up */
#else
#define	never	0		/* some <assert.h>s have bugs too */
#endif

/*
 - regcomp - interface for parser and compilation
 = API_EXPORT(int) regcomp(regex_t *, const char *, int);
 = #define	REG_BASIC	0000
 = #define	REG_EXTENDED	0001
 = #define	REG_ICASE	0002
 = #define	REG_NOSUB	0004
 = #define	REG_NEWLINE	0010
 = #define	REG_NOSPEC	0020
 = #define	REG_PEND	0040
 = #define	REG_DUMP	0200
 */
/*
 - p_ere - ERE parser top level, concatenation and alternation
 == static void p_ere(register struct parse *p, int stop);
 */
static void
p_ere(register struct parse *p, int stop)
{
	register char c;
	register sopno prevback = 0;
	register sopno prevfwd = 0;
	register sopno conc;
	register int first = 1;		/* is this the first alternative? */

	for (;;) {
		/* do a bunch of concatenated expressions */
		conc = HERE();
		while (MORE() && (c = PEEK()) != '|' && c != stop)
			p_ere_exp(p);
		(void) REQUIRE(HERE() != conc, REG_EMPTY);	/* require nonempty */

		if (!EAT('|'))
			break;		/* NOTE BREAK OUT */

		if (first) {
			INSERT(OCH_, conc);	/* offset is wrong */
			prevfwd = conc;
			prevback = conc;
			first = 0;
		}
		ASTERN(OOR1, prevback);
		prevback = THERE();
		AHEAD(prevfwd);			/* fix previous offset */
		prevfwd = HERE();
		EMIT(OOR2, 0);			/* offset is very wrong */
	}

	if (!first) {		/* tail-end fixups */
		AHEAD(prevfwd);
		ASTERN(O_CH, prevback);
	}

	assert(!MORE() || SEE(stop));
}

/*
 - p_ere_exp - parse one subERE, an atom possibly followed by a repetition op
 == static void p_ere_exp(register struct parse *p);
 */
static void
p_ere_exp(register struct parse *p)
{
	register char c;
	register sopno pos;
	register int count;
	register int count2;
	register sopno subno;
	int wascaret = 0;

	assert(MORE());		/* caller should have ensured this */
	c = GETNEXT();

	pos = HERE();
	switch (c) {
	case '(':
		REQUIRE(MORE(), REG_EPAREN);
		p->g->nsub++;
		subno = p->g->nsub;
		if (subno < NPAREN)
			p->pbegin[subno] = HERE();
		EMIT(OLPAREN, subno);
		if (!SEE(')'))
			p_ere(p, ')');
		if (subno < NPAREN) {
			p->pend[subno] = HERE();
			assert(p->pend[subno] != 0);
		}
		EMIT(ORPAREN, subno);
		MUSTEAT(')', REG_EPAREN);
		break;
#ifndef POSIX_MISTAKE
	case ')':		/* happens only if no current unmatched ( */
		/*
		 * You may ask, why the ifndef?  Because I didn't notice
		 * this until slightly too late for 1003.2, and none of the
		 * other 1003.2 regular-expression reviewers noticed it at
		 * all.  So an unmatched ) is legal POSIX, at least until
		 * we can get it fixed.
		 */
		SETERROR(REG_EPAREN);
		break;
#endif
	case '^':
		EMIT(OBOL, 0);
		p->g->iflags |= USEBOL;
		p->g->nbol++;
		wascaret = 1;
		break;
	case '$':
		EMIT(OEOL, 0);
		p->g->iflags |= USEEOL;
		p->g->neol++;
		break;
	case '|':
		SETERROR(REG_EMPTY);
		break;
	case '*':
	case '+':
	case '?':
		SETERROR(REG_BADRPT);
		break;
	case '.':
		if (p->g->cflags&REG_NEWLINE)
			nonnewline(p);
		else
			EMIT(OANY, 0);
		break;
	case '[':
		p_bracket(p);
		break;
	case '\\':
		REQUIRE(MORE(), REG_EESCAPE);
		c = GETNEXT();
		ordinary(p, c);
		break;
	case '{':		/* okay as ordinary except if digit follows */
		REQUIRE(!MORE() || !isdigit(PEEK()), REG_BADRPT);
		/* FALLTHROUGH */
	default:
		ordinary(p, c);
		break;
	}

	if (!MORE())
		return;
	c = PEEK();
	/* we call { a repetition if followed by a digit */
	if (!( c == '*' || c == '+' || c == '?' ||
				(c == '{' && MORE2() && isdigit(PEEK2())) ))
		return;		/* no repetition, we're done */
	NEXT();

	REQUIRE(!wascaret, REG_BADRPT);
	switch (c) {
	case '*':	/* implemented as +? */
		/* this case does not require the (y|) trick, noKLUDGE */
		INSERT(OPLUS_, pos);
		ASTERN(O_PLUS, pos);
		INSERT(OQUEST_, pos);
		ASTERN(O_QUEST, pos);
		break;
	case '+':
		INSERT(OPLUS_, pos);
		ASTERN(O_PLUS, pos);
		break;
	case '?':
		/* KLUDGE: emit y? as (y|) until subtle bug gets fixed */
		INSERT(OCH_, pos);		/* offset slightly wrong */
		ASTERN(OOR1, pos);		/* this one's right */
		AHEAD(pos);			/* fix the OCH_ */
		EMIT(OOR2, 0);			/* offset very wrong... */
		AHEAD(THERE());			/* ...so fix it */
		ASTERN(O_CH, THERETHERE());
		break;
	case '{':
		count = p_count(p);
		if (EAT(',')) {
			if (isdigit(PEEK())) {
				count2 = p_count(p);
				REQUIRE(count <= count2, REG_BADBR);
			} else		/* single number with comma */
				count2 = INFINITY;
		} else		/* just a single number */
			count2 = count;
		repeat(p, pos, count, count2);
		if (!EAT('}')) {	/* error heuristics */
			while (MORE() && PEEK() != '}')
				NEXT();
			REQUIRE(MORE(), REG_EBRACE);
			SETERROR(REG_BADBR);
		}
		break;
	}

	if (!MORE())
		return;
	c = PEEK();
	if (!( c == '*' || c == '+' || c == '?' ||
				(c == '{' && MORE2() && isdigit(PEEK2())) ) )
		return;
	SETERROR(REG_BADRPT);
}

/*
 - p_str - string (no metacharacters) "parser"
 == static void p_str(register struct parse *p);
 */
static void
p_str(register struct parse *p)
{
	REQUIRE(MORE(), REG_EMPTY);
	while (MORE())
		ordinary(p, GETNEXT());
}

/*
 - p_bre - BRE parser top level, anchoring and concatenation
 == static void p_bre(register struct parse *p, register int end1, \
 ==	register int end2);
 * Giving end1 as OUT essentially eliminates the end1/end2 check.
 *
 * This implementation is a bit of a kludge, in that a trailing $ is first
 * taken as an ordinary character and then revised to be an anchor.  The
 * only undesirable side effect is that '$' gets included as a character
 * category in such cases.  This is fairly harmless; not worth fixing.
 * The amount of lookahead needed to avoid this kludge is excessive.
 */
static void
p_bre(register struct parse *p, register int end1, register int end2)
{
	register sopno start = HERE();
	register int first = 1;			/* first subexpression? */
	register int wasdollar = 0;

	if (EAT('^')) {
		EMIT(OBOL, 0);
		p->g->iflags |= USEBOL;
		p->g->nbol++;
	}
	while (MORE() && !SEETWO(end1, end2)) {
		wasdollar = p_simp_re(p, first);
		first = 0;
	}
	if (wasdollar) {	/* oops, that was a trailing anchor */
		DROP(1);
		EMIT(OEOL, 0);
		p->g->iflags |= USEEOL;
		p->g->neol++;
	}

	REQUIRE(HERE() != start, REG_EMPTY);	/* require nonempty */
}

/*
 - p_simp_re - parse a simple RE, an atom possibly followed by a repetition
 == static int p_simp_re(register struct parse *p, int starordinary);
 */
static int			/* was the simple RE an unbackslashed $? */
p_simp_re(register struct parse *p, int starordinary)
{
	register int c;
	register int count;
	register int count2;
	register sopno pos;
	register int i;
	register sopno subno;
#	define	BACKSL	(1<<CHAR_BIT)

	pos = HERE();		/* repetion op, if any, covers from here */

	assert(MORE());		/* caller should have ensured this */
	c = GETNEXT();
	if (c == '\\') {
		REQUIRE(MORE(), REG_EESCAPE);
		c = BACKSL | (unsigned char)GETNEXT();
	}
	switch (c) {
	case '.':
		if (p->g->cflags&REG_NEWLINE)
			nonnewline(p);
		else
			EMIT(OANY, 0);
		break;
	case '[':
		p_bracket(p);
		break;
	case BACKSL|'{':
		SETERROR(REG_BADRPT);
		break;
	case BACKSL|'(':
		p->g->nsub++;
		subno = p->g->nsub;
		if (subno < NPAREN)
			p->pbegin[subno] = HERE();
		EMIT(OLPAREN, subno);
		/* the MORE here is an error heuristic */
		if (MORE() && !SEETWO('\\', ')'))
			p_bre(p, '\\', ')');
		if (subno < NPAREN) {
			p->pend[subno] = HERE();
			assert(p->pend[subno] != 0);
		}
		EMIT(ORPAREN, subno);
		REQUIRE(EATTWO('\\', ')'), REG_EPAREN);
		break;
	case BACKSL|')':	/* should not get here -- must be user */
	case BACKSL|'}':
		SETERROR(REG_EPAREN);
		break;
	case BACKSL|'1':
	case BACKSL|'2':
	case BACKSL|'3':
	case BACKSL|'4':
	case BACKSL|'5':
	case BACKSL|'6':
	case BACKSL|'7':
	case BACKSL|'8':
	case BACKSL|'9':
		i = (c&~BACKSL) - '0';
		assert(i < NPAREN);
		if (p->pend[i] != 0) {
			assert((unsigned)i <= p->g->nsub);
			EMIT(OBACK_, i);
			assert(p->pbegin[i] != 0);
			assert(OP(p->strip[p->pbegin[i]]) == OLPAREN);
			assert(OP(p->strip[p->pend[i]]) == ORPAREN);
			(void) dupl(p, p->pbegin[i]+1, p->pend[i]);
			EMIT(O_BACK, i);
		} else
			SETERROR(REG_ESUBREG);
		p->g->backrefs = 1;
		break;
	case '*':
		REQUIRE(starordinary, REG_BADRPT);
		/* FALLTHROUGH */
	default:
		ordinary(p, (char)c);	/* takes off BACKSL, if any */
		break;
	}

	if (EAT('*')) {		/* implemented as +? */
		/* this case does not require the (y|) trick, noKLUDGE */
		INSERT(OPLUS_, pos);
		ASTERN(O_PLUS, pos);
		INSERT(OQUEST_, pos);
		ASTERN(O_QUEST, pos);
	} else if (EATTWO('\\', '{')) {
		count = p_count(p);
		if (EAT(',')) {
			if (MORE() && isdigit(PEEK())) {
				count2 = p_count(p);
				REQUIRE(count <= count2, REG_BADBR);
			} else		/* single number with comma */
				count2 = INFINITY;
		} else		/* just a single number */
			count2 = count;
		repeat(p, pos, count, count2);
		if (!EATTWO('\\', '}')) {	/* error heuristics */
			while (MORE() && !SEETWO('\\', '}'))
				NEXT();
			REQUIRE(MORE(), REG_EBRACE);
			SETERROR(REG_BADBR);
		}
	} else if (c == (unsigned char)'$')	/* $ (but not \$) ends it */
		return(1);

	return(0);
}

/*
 - p_count - parse a repetition count
 == static int p_count(register struct parse *p);
 */
static int			/* the value */
p_count(register struct parse *p)
{
	register int count = 0;
	register int ndigits = 0;

	while (MORE() && isdigit(PEEK()) && count <= DUPMAX) {
		count = count*10 + (GETNEXT() - '0');
		ndigits++;
	}

	REQUIRE(ndigits > 0 && count <= DUPMAX, REG_BADBR);
	return(count);
}

/*
 - p_bracket - parse a bracketed character list
 == static void p_bracket(register struct parse *p);
 *
 * Note a significant property of this code:  if the allocset() did SETERROR,
 * no set operations are done.
 */
static void
p_bracket(register struct parse *p)
{
	register cset *cs = allocset(p);
	register int invert = 0;

	/* Dept of Truly Sickening Special-Case Kludges */
	if (p->next + 5 < p->end && strncmp(p->next, "[:<:]]", 6) == 0) {
		EMIT(OBOW, 0);
		NEXTn(6);
		return;
	}
	if (p->next + 5 < p->end && strncmp(p->next, "[:>:]]", 6) == 0) {
		EMIT(OEOW, 0);
		NEXTn(6);
		return;
	}

	if (EAT('^'))
		invert++;	/* make note to invert set at end */
	if (EAT(']'))
		CHadd(cs, ']');
	else if (EAT('-'))
		CHadd(cs, '-');
	while (MORE() && PEEK() != ']' && !SEETWO('-', ']'))
		p_b_term(p, cs);
	if (EAT('-'))
		CHadd(cs, '-');
	MUSTEAT(']', REG_EBRACK);

	if (p->error != 0)	/* don't mess things up further */
		return;

	if (p->g->cflags&REG_ICASE) {
		register int i;
		register int ci;

		for (i = p->g->csetsize - 1; i >= 0; i--)
			if (CHIN(cs, i) && isalpha(i)) {
				ci = othercase(i);
				if (ci != i)
					CHadd(cs, ci);
			}
		if (cs->multis != NULL)
			mccase(p, cs);
	}
	if (invert) {
		register int i;

		for (i = p->g->csetsize - 1; i >= 0; i--)
			if (CHIN(cs, i))
				CHsub(cs, i);
			else
				CHadd(cs, i);
		if (p->g->cflags&REG_NEWLINE)
			CHsub(cs, '\n');
		if (cs->multis != NULL)
			mcinvert(p, cs);
	}

	assert(cs->multis == NULL);		/* xxx */

	if (nch(p, cs) == 1) {		/* optimize singleton sets */
		ordinary(p, firstch(p, cs));
		freeset(p, cs);
	} else
		EMIT(OANYOF, freezeset(p, cs));
}

/*
 - p_b_term - parse one term of a bracketed character list
 == static void p_b_term(register struct parse *p, register cset *cs);
 */
static void
p_b_term(register struct parse *p, register cset *cs)
{
	register char c;
	register char start, finish;
	register int i;

	/* classify what we've got */
	switch ((MORE()) ? PEEK() : '\0') {
	case '[':
		c = (MORE2()) ? PEEK2() : '\0';
		break;
	case '-':
		SETERROR(REG_ERANGE);
		return;			/* NOTE RETURN */
		break;
	default:
		c = '\0';
		break;
	}

	switch (c) {
	case ':':		/* character class */
		NEXT2();
		REQUIRE(MORE(), REG_EBRACK);
		c = PEEK();
		REQUIRE(c != '-' && c != ']', REG_ECTYPE);
		p_b_cclass(p, cs);
		REQUIRE(MORE(), REG_EBRACK);
		REQUIRE(EATTWO(':', ']'), REG_ECTYPE);
		break;
	case '=':		/* equivalence class */
		NEXT2();
		REQUIRE(MORE(), REG_EBRACK);
		c = PEEK();
		REQUIRE(c != '-' && c != ']', REG_ECOLLATE);
		p_b_eclass(p, cs);
		REQUIRE(MORE(), REG_EBRACK);
		REQUIRE(EATTWO('=', ']'), REG_ECOLLATE);
		break;
	default:		/* symbol, ordinary character, or range */
/* xxx revision needed for multichar stuff */
		start = p_b_symbol(p);
		if (SEE('-') && MORE2() && PEEK2() != ']') {
			/* range */
			NEXT();
			if (EAT('-'))
				finish = '-';
			else
				finish = p_b_symbol(p);
		} else
			finish = start;
/* xxx what about signed chars here... */
		REQUIRE(start <= finish, REG_ERANGE);
		for (i = start; i <= finish; i++)
			CHadd(cs, i);
		break;
	}
}

/*
 - p_b_cclass - parse a character-class name and deal with it
 == static void p_b_cclass(register struct parse *p, register cset *cs);
 */
static void
p_b_cclass(register struct parse *p, register cset *cs)
{
	register char *sp = p->next;
	register struct cclass *cp;
	register size_t len;
	register char *u;
	register char c;

	while (MORE() && isalpha(PEEK()))
		NEXT();
	len = p->next - sp;
	for (cp = cclasses; cp->name != NULL; cp++)
		if (strncmp(cp->name, sp, len) == 0 && cp->name[len] == '\0')
			break;
	if (cp->name == NULL) {
		/* oops, didn't find it */
		SETERROR(REG_ECTYPE);
		return;
	}

	u = cp->chars;
	while ((c = *u++) != '\0')
		CHadd(cs, c);
	for (u = cp->multis; *u != '\0'; u += strlen(u) + 1)
		MCadd(p, cs, u);
}

/*
 - p_b_eclass - parse an equivalence-class name and deal with it
 == static void p_b_eclass(register struct parse *p, register cset *cs);
 *
 * This implementation is incomplete. xxx
 */
static void
p_b_eclass(register struct parse *p, register cset *cs)
{
	register char c;

	c = p_b_coll_elem(p, '=');
	CHadd(cs, c);
}

/*
 - p_b_symbol - parse a character or [..]ed multicharacter collating symbol
 == static char p_b_symbol(register struct parse *p);
 */
static char			/* value of symbol */
p_b_symbol(register struct parse *p)
{
	register char value;

	REQUIRE(MORE(), REG_EBRACK);
	if (!EATTWO('[', '.'))
		return(GETNEXT());

	/* collating symbol */
	value = p_b_coll_elem(p, '.');
	REQUIRE(EATTWO('.', ']'), REG_ECOLLATE);
	return(value);
}

/*
 - p_b_coll_elem - parse a collating-element name and look it up
 == static char p_b_coll_elem(register struct parse *p, int endc);
 */
static char			/* value of collating element */
p_b_coll_elem(register struct parse *p, int endc)
{
	register char *sp = p->next;
	register struct cname *cp;
	register int len;

	while (MORE() && !SEETWO(endc, ']'))
		NEXT();
	if (!MORE()) {
		SETERROR(REG_EBRACK);
		return(0);
	}
	len = p->next - sp;
	for (cp = cnames; cp->name != NULL; cp++)
		if (strncmp(cp->name, sp, len) == 0 && cp->name[len] == '\0')
			return(cp->code);	/* known name */
	if (len == 1)
		return(*sp);	/* single character */
	SETERROR(REG_ECOLLATE);			/* neither */
	return(0);
}

/*
 - othercase - return the case counterpart of an alphabetic
 == static char othercase(int ch);
 */
static char			/* if no counterpart, return ch */
othercase(int ch)
{
	assert(isalpha(ch));
	if (isupper(ch))
		return(tolower(ch));
	else if (islower(ch))
		return(toupper(ch));
	else			/* peculiar, but could happen */
		return(ch);
}

/*
 - bothcases - emit a dualcase version of a two-case character
 == static void bothcases(register struct parse *p, int ch);
 *
 * Boy, is this implementation ever a kludge...
 */
static void
bothcases(register struct parse *p, int ch)
{
	register char *oldnext = p->next;
	register char *oldend = p->end;
	char bracket[3];

	assert(othercase(ch) != ch);	/* p_bracket() would recurse */
	p->next = bracket;
	p->end = bracket+2;
	bracket[0] = ch;
	bracket[1] = ']';
	bracket[2] = '\0';
	p_bracket(p);
	assert(p->next == bracket+2);
	p->next = oldnext;
	p->end = oldend;
}

/*
 - ordinary - emit an ordinary character
 == static void ordinary(register struct parse *p, register int ch);
 */
static void
ordinary(register struct parse *p, register int ch)
{
	register cat_t *cap = p->g->categories;

	if ((p->g->cflags&REG_ICASE) && isalpha(ch) && othercase(ch) != ch)
		bothcases(p, ch);
	else {
		EMIT(OCHAR, (unsigned char)ch);
		if (cap[ch] == 0)
			cap[ch] = p->g->ncategories++;
	}
}

/*
 - nonnewline - emit REG_NEWLINE version of OANY
 == static void nonnewline(register struct parse *p);
 *
 * Boy, is this implementation ever a kludge...
 */
static void
nonnewline(register struct parse *p)
{
	register char *oldnext = p->next;
	register char *oldend = p->end;
	char bracket[4];

	p->next = bracket;
	p->end = bracket+3;
	bracket[0] = '^';
	bracket[1] = '\n';
	bracket[2] = ']';
	bracket[3] = '\0';
	p_bracket(p);
	assert(p->next == bracket+3);
	p->next = oldnext;
	p->end = oldend;
}

/*
 - repeat - generate code for a bounded repetition, recursively if needed
 == static void repeat(register struct parse *p, sopno start, int from, int to);
 */
static void
repeat(register struct parse *p, sopno start, int from, int to)
{
	register sopno finish = HERE();
#	define	N	2
#	define	INF	3
#	define	REP(f, t)	((f)*8 + (t))
#	define	MAP(n)	(((n) <= 1) ? (n) : ((n) == INFINITY) ? INF : N)
	register sopno copy;

	if (p->error != 0)	/* head off possible runaway recursion */
		return;

	assert(from <= to);

	switch (REP(MAP(from), MAP(to))) {
	case REP(0, 0):			/* must be user doing this */
		DROP(finish-start);	/* drop the operand */
		break;
	case REP(0, 1):			/* as x{1,1}? */
	case REP(0, N):			/* as x{1,n}? */
	case REP(0, INF):		/* as x{1,}? */
		/* KLUDGE: emit y? as (y|) until subtle bug gets fixed */
		INSERT(OCH_, start);		/* offset is wrong... */
		repeat(p, start+1, 1, to);
		ASTERN(OOR1, start);
		AHEAD(start);			/* ... fix it */
		EMIT(OOR2, 0);
		AHEAD(THERE());
		ASTERN(O_CH, THERETHERE());
		break;
	case REP(1, 1):			/* trivial case */
		/* done */
		break;
	case REP(1, N):			/* as x?x{1,n-1} */
		/* KLUDGE: emit y? as (y|) until subtle bug gets fixed */
		INSERT(OCH_, start);
		ASTERN(OOR1, start);
		AHEAD(start);
		EMIT(OOR2, 0);			/* offset very wrong... */
		AHEAD(THERE());			/* ...so fix it */
		ASTERN(O_CH, THERETHERE());
		copy = dupl(p, start+1, finish+1);
		assert(copy == finish+4);
		repeat(p, copy, 1, to-1);
		break;
	case REP(1, INF):		/* as x+ */
		INSERT(OPLUS_, start);
		ASTERN(O_PLUS, start);
		break;
	case REP(N, N):			/* as xx{m-1,n-1} */
		copy = dupl(p, start, finish);
		repeat(p, copy, from-1, to-1);
		break;
	case REP(N, INF):		/* as xx{n-1,INF} */
		copy = dupl(p, start, finish);
		repeat(p, copy, from-1, to);
		break;
	default:			/* "can't happen" */
		SETERROR(REG_ASSERT);	/* just in case */
		break;
	}
}

/*
 - seterr - set an error condition
 == static int seterr(register struct parse *p, int e);
 */
static int			/* useless but makes type checking happy */
seterr(register struct parse *p, int e)
{
	if (p->error == 0)	/* keep earliest error condition */
		p->error = e;
	p->next = nuls;		/* try to bring things to a halt */
	p->end = nuls;
	return(0);		/* make the return value well-defined */
}

/*
 - allocset - allocate a set of characters for []
 == static cset *allocset(register struct parse *p);
 */
static cset *
allocset(register struct parse *p)
{
	register int no = p->g->ncsets++;
	register size_t nc;
	register size_t nbytes;
	register cset *cs;
	register size_t css = (size_t)p->g->csetsize;
	register int i;

	if (no >= p->ncsalloc) {	/* need another column of space */
		p->ncsalloc += CHAR_BIT;
		nc = p->ncsalloc;
		assert(nc % CHAR_BIT == 0);
		nbytes = nc / CHAR_BIT * css;
		if (p->g->sets == NULL)
			p->g->sets = (cset *)malloc(nc * sizeof(cset));
		else
			p->g->sets = (cset *)realloc((char *)p->g->sets,
							nc * sizeof(cset));
		if (p->g->setbits == NULL)
			p->g->setbits = (uch *)malloc(nbytes);
		else {
			p->g->setbits = (uch *)realloc((char *)p->g->setbits,
								nbytes);
			/* xxx this isn't right if setbits is now NULL */
			for (i = 0; i < no; i++)
				p->g->sets[i].ptr = p->g->setbits + css*(i/CHAR_BIT);
		}
		if (p->g->sets != NULL && p->g->setbits != NULL)
			(void) memset((char *)p->g->setbits + (nbytes - css),
								0, css);
		else {
			no = 0;
			SETERROR(REG_ESPACE);
			/* caller's responsibility not to do set ops */
		}
	}

	assert(p->g->sets != NULL);	/* xxx */
	cs = &p->g->sets[no];
	cs->ptr = p->g->setbits + css*((no)/CHAR_BIT);
	cs->mask = 1 << ((no) % CHAR_BIT);
	cs->hash = 0;
	cs->smultis = 0;
	cs->multis = NULL;

	return(cs);
}

/*
 - freeset - free a now-unused set
 == static void freeset(register struct parse *p, register cset *cs);
 */
static void
freeset(register struct parse *p, register cset *cs)
{
	register size_t i;
	register cset *top = &p->g->sets[p->g->ncsets];
	register size_t css = (size_t)p->g->csetsize;

	for (i = 0; i < css; i++)
		CHsub(cs, i);
	if (cs == top-1)	/* recover only the easy case */
		p->g->ncsets--;
}

/*
 - freezeset - final processing on a set of characters
 == static int freezeset(register struct parse *p, register cset *cs);
 *
 * The main task here is merging identical sets.  This is usually a waste
 * of time (although the hash code minimizes the overhead), but can win
 * big if REG_ICASE is being used.  REG_ICASE, by the way, is why the hash
 * is done using addition rather than xor -- all ASCII [aA] sets xor to
 * the same value!
 */
static int			/* set number */
freezeset(register struct parse *p, register cset *cs)
{
	register uch h = cs->hash;
	register size_t i;
	register cset *top = &p->g->sets[p->g->ncsets];
	register cset *cs2;
	register size_t css = (size_t)p->g->csetsize;

	/* look for an earlier one which is the same */
	for (cs2 = &p->g->sets[0]; cs2 < top; cs2++)
		if (cs2->hash == h && cs2 != cs) {
			/* maybe */
			for (i = 0; i < css; i++)
				if (!!CHIN(cs2, i) != !!CHIN(cs, i))
					break;		/* no */
			if (i == css)
				break;			/* yes */
		}

	if (cs2 < top) {	/* found one */
		freeset(p, cs);
		cs = cs2;
	}

	return((int)(cs - p->g->sets));
}

/*
 - firstch - return first character in a set (which must have at least one)
 == static int firstch(register struct parse *p, register cset *cs);
 */
static int			/* character; there is no "none" value */
firstch(register struct parse *p, register cset *cs)
{
	register size_t i;
	register size_t css = (size_t)p->g->csetsize;

	for (i = 0; i < css; i++)
		if (CHIN(cs, i))
			return((char)i);
	assert(never);
	return(0);		/* arbitrary */
}

/*
 - nch - number of characters in a set
 == static int nch(register struct parse *p, register cset *cs);
 */
static int
nch(register struct parse *p, register cset *cs)
{
	register size_t i;
	register size_t css = (size_t)p->g->csetsize;
	register int n = 0;

	for (i = 0; i < css; i++)
		if (CHIN(cs, i))
			n++;
	return(n);
}

/*
 - mcadd - add a collating element to a cset
 == static void mcadd(register struct parse *p, register cset *cs, \
 ==	register char *cp);
 */
static void
mcadd(register struct parse *p, register cset *cs, register char *cp)
{
	register size_t oldend = cs->smultis;

	cs->smultis += strlen(cp) + 1;
	if (cs->multis == NULL)
		cs->multis = (char *)malloc(cs->smultis);
	else
		cs->multis = (char *)realloc(cs->multis, cs->smultis);
	if (cs->multis == NULL) {
		SETERROR(REG_ESPACE);
		return;
	}

	(void) strcpy(cs->multis + oldend - 1, cp);
	cs->multis[cs->smultis - 1] = '\0';
}

#if 0
/*
 - mcsub - subtract a collating element from a cset
 == static void mcsub(register cset *cs, register char *cp);
 */
static void
mcsub(register cset *cs, register char *cp)
{
	register char *fp = mcfind(cs, cp);
	register size_t len = strlen(fp);

	assert(fp != NULL);
	(void) memmove(fp, fp + len + 1,
				cs->smultis - (fp + len + 1 - cs->multis));
	cs->smultis -= len;

	if (cs->smultis == 0) {
		free(cs->multis);
		cs->multis = NULL;
		return;
	}

	cs->multis = realloc(cs->multis, cs->smultis);
	assert(cs->multis != NULL);
}

/*
 - mcin - is a collating element in a cset?
 == static int mcin(register cset *cs, register char *cp);
 */
static int
mcin(register cset *cs, register char *cp)
{
	return(mcfind(cs, cp) != NULL);
}


/*
 - mcfind - find a collating element in a cset
 == static char *mcfind(register cset *cs, register char *cp);
 */
static char *
mcfind(register cset *cs, register char *cp)
{
	register char *p;

	if (cs->multis == NULL)
		return(NULL);
	for (p = cs->multis; *p != '\0'; p += strlen(p) + 1)
		if (strcmp(cp, p) == 0)
			return(p);
	return(NULL);
}
#endif

/*
 - mcinvert - invert the list of collating elements in a cset
 == static void mcinvert(register struct parse *p, register cset *cs);
 *
 * This would have to know the set of possibilities.  Implementation
 * is deferred.
 */
static void
mcinvert(register struct parse *p, register cset *cs)
{
	assert(cs->multis == NULL);	/* xxx */
}

/*
 - mccase - add case counterparts of the list of collating elements in a cset
 == static void mccase(register struct parse *p, register cset *cs);
 *
 * This would have to know the set of possibilities.  Implementation
 * is deferred.
 */
static void
mccase(register struct parse *p, register cset *cs)
{
	assert(cs->multis == NULL);	/* xxx */
}

/*
 - isinsets - is this character in any sets?
 == static int isinsets(register struct re_guts *g, int c);
 */
static int			/* predicate */
isinsets(register struct re_guts *g, int c)
{
	register uch *col;
	register int i;
	register int ncols = (g->ncsets+(CHAR_BIT-1)) / CHAR_BIT;
	register unsigned uc = (unsigned char)c;

	for (i = 0, col = g->setbits; i < ncols; i++, col += g->csetsize)
		if (col[uc] != 0)
			return(1);
	return(0);
}

/*
 - samesets - are these two characters in exactly the same sets?
 == static int samesets(register struct re_guts *g, int c1, int c2);
 */
static int			/* predicate */
samesets(register struct re_guts *g, int c1, int c2)
{
	register uch *col;
	register int i;
	register int ncols = (g->ncsets+(CHAR_BIT-1)) / CHAR_BIT;
	register unsigned uc1 = (unsigned char)c1;
	register unsigned uc2 = (unsigned char)c2;

	for (i = 0, col = g->setbits; i < ncols; i++, col += g->csetsize)
		if (col[uc1] != col[uc2])
			return(0);
	return(1);
}

/*
 - categorize - sort out character categories
 == static void categorize(struct parse *p, register struct re_guts *g);
 */
static void
categorize(struct parse *p, register struct re_guts *g)
{
	register cat_t *cats = g->categories;
	register int c;
	register int c2;
	register cat_t cat;

	/* avoid making error situations worse */
	if (p->error != 0)
		return;

	for (c = CHAR_MIN; c <= CHAR_MAX; c++)
		if (cats[c] == 0 && isinsets(g, c)) {
			cat = g->ncategories++;
			cats[c] = cat;
			for (c2 = c+1; c2 <= CHAR_MAX; c2++)
				if (cats[c2] == 0 && samesets(g, c, c2))
					cats[c2] = cat;
		}
}

/*
 - dupl - emit a duplicate of a bunch of sops
 == static sopno dupl(register struct parse *p, sopno start, sopno finish);
 */
static sopno			/* start of duplicate */
dupl(register struct parse *p, sopno start, sopno finish)
{
	register sopno ret = HERE();
	register sopno len = finish - start;

	assert(finish >= start);
	if (len == 0)
		return(ret);
	enlarge(p, p->ssize + len);	/* this many unexpected additions */
	assert(p->ssize >= p->slen + len);
	(void) memcpy((char *)(p->strip + p->slen),
		(char *)(p->strip + start), (size_t)len*sizeof(sop));
	p->slen += len;
	return(ret);
}

/*
 - doemit - emit a strip operator
 == static void doemit(register struct parse *p, sop op, size_t opnd);
 *
 * It might seem better to implement this as a macro with a function as
 * hard-case backup, but it's just too big and messy unless there are
 * some changes to the data structures.  Maybe later.
 */
static void
doemit(register struct parse *p, sop op, size_t opnd)
{
	/* avoid making error situations worse */
	if (p->error != 0)
		return;

	/* deal with oversize operands ("can't happen", more or less) */
	assert(opnd < 1<<OPSHIFT);

	/* deal with undersized strip */
	if (p->slen >= p->ssize)
		enlarge(p, (p->ssize+1) / 2 * 3);	/* +50% */
	assert(p->slen < p->ssize);

	/* finally, it's all reduced to the easy case */
	p->strip[p->slen++] = SOP(op, opnd);
}

/*
 - doinsert - insert a sop into the strip
 == static void doinsert(register struct parse *p, sop op, size_t opnd, sopno pos);
 */
static void
doinsert(register struct parse *p, sop op, size_t opnd, sopno pos)
{
	register sopno sn;
	register sop s;
	register int i;

	/* avoid making error situations worse */
	if (p->error != 0)
		return;

	sn = HERE();
	EMIT(op, opnd);		/* do checks, ensure space */
	assert(HERE() == sn+1);
	s = p->strip[sn];

	/* adjust paren pointers */
	assert(pos > 0);
	for (i = 1; i < NPAREN; i++) {
		if (p->pbegin[i] >= pos) {
			p->pbegin[i]++;
		}
		if (p->pend[i] >= pos) {
			p->pend[i]++;
		}
	}

	memmove((char *)&p->strip[pos+1], (char *)&p->strip[pos],
						(HERE()-pos-1)*sizeof(sop));
	p->strip[pos] = s;
}

/*
 - dofwd - complete a forward reference
 == static void dofwd(register struct parse *p, sopno pos, sop value);
 */
static void
dofwd(register struct parse *p, register sopno pos, sop value)
{
	/* avoid making error situations worse */
	if (p->error != 0)
		return;

	assert(value < 1<<OPSHIFT);
	p->strip[pos] = OP(p->strip[pos]) | value;
}

/*
 - enlarge - enlarge the strip
 == static void enlarge(register struct parse *p, sopno size);
 */
static void
enlarge(register struct parse *p, register sopno size)
{
	register sop *sp;

	if (p->ssize >= size)
		return;

	sp = (sop *)realloc(p->strip, size*sizeof(sop));
	if (sp == NULL) {
		SETERROR(REG_ESPACE);
		return;
	}
	p->strip = sp;
	p->ssize = size;
}

/*
 - stripsnug - compact the strip
 == static void stripsnug(register struct parse *p, register struct re_guts *g);
 */
static void
stripsnug(register struct parse *p, register struct re_guts *g)
{
	g->nstates = p->slen;
	g->strip = (sop *)realloc((char *)p->strip, p->slen * sizeof(sop));
	if (g->strip == NULL) {
		SETERROR(REG_ESPACE);
		g->strip = p->strip;
	}
}

/*
 - findmust - fill in must and mlen with longest mandatory literal string
 == static void findmust(register struct parse *p, register struct re_guts *g);
 *
 * This algorithm could do fancy things like analyzing the operands of |
 * for common subsequences.  Someday.  This code is simple and finds most
 * of the interesting cases.
 *
 * Note that must and mlen got initialized during setup.
 */
static void
findmust(struct parse *p, register struct re_guts *g)
{
	register sop *scan;
	sop *start = NULL;
	register sop *newstart = NULL;
	register sopno newlen;
	register sop s;
	register char *cp;
	register sopno i;

	/* avoid making error situations worse */
	if (p->error != 0)
		return;

	/* find the longest OCHAR sequence in strip */
	newlen = 0;
	scan = g->strip + 1;
	do {
		s = *scan++;
		switch (OP(s)) {
		case OCHAR:		/* sequence member */
			if (newlen == 0)		/* new sequence */
				newstart = scan - 1;
			newlen++;
			break;
		case OPLUS_:		/* things that don't break one */
		case OLPAREN:
		case ORPAREN:
			break;
		case OQUEST_:		/* things that must be skipped */
		case OCH_:
			scan--;
			do {
				scan += OPND(s);
				s = *scan;
				/* assert() interferes w debug printouts */
				if (OP(s) != O_QUEST && OP(s) != O_CH &&
							OP(s) != OOR2) {
					g->iflags |= BAD;
					return;
				}
			} while (OP(s) != O_QUEST && OP(s) != O_CH);
			/* fallthrough */
		default:		/* things that break a sequence */
			if (newlen > g->mlen) {		/* ends one */
				start = newstart;
				g->mlen = newlen;
			}
			newlen = 0;
			break;
		}
	} while (OP(s) != OEND);

	if (g->mlen == 0)		/* there isn't one */
		return;

	/* turn it into a character string */
	g->must = (char *)malloc((size_t)g->mlen + 1);
	if (g->must == NULL) {		/* argh; just forget it */
		g->mlen = 0;
		return;
	}
	cp = g->must;
	scan = start;
	for (i = g->mlen; i > 0; i--) {
		while (OP(s = *scan++) != OCHAR)
			continue;
		assert(cp < g->must + g->mlen);
		*cp++ = (char)OPND(s);
	}
	assert(cp == g->must + g->mlen);
	*cp++ = '\0';		/* just on general principles */
}

/*
 - pluscount - count + nesting
 == static sopno pluscount(register struct parse *p, register struct re_guts *g);
 */
static sopno			/* nesting depth */
pluscount(struct parse *p, register struct re_guts *g)
{
	register sop *scan;
	register sop s;
	register sopno plusnest = 0;
	register sopno maxnest = 0;

	if (p->error != 0)
		return(0);	/* there may not be an OEND */

	scan = g->strip + 1;
	do {
		s = *scan++;
		switch (OP(s)) {
		case OPLUS_:
			plusnest++;
			break;
		case O_PLUS:
			if (plusnest > maxnest)
				maxnest = plusnest;
			plusnest--;
			break;
		}
	} while (OP(s) != OEND);
	if (plusnest != 0)
		g->iflags |= BAD;
	return(maxnest);
}

API_EXPORT(int)			/* 0 success, otherwise REG_something */
regcomp(regex_t *preg, const char *pattern, int cflags)
{
	struct parse pa;
	register struct re_guts *g;
	register struct parse *p = &pa;
	register int i;
	register size_t len;
#ifdef REDEBUG
#	define	GOODFLAGS(f)	(f)
#else
#	define	GOODFLAGS(f)	((f)&~REG_DUMP)
#endif

	cflags = GOODFLAGS(cflags);
	if ((cflags&REG_EXTENDED) && (cflags&REG_NOSPEC))
		return(REG_INVARG);

	if (cflags&REG_PEND) {
		if (preg->re_endp < pattern)
			return(REG_INVARG);
		len = preg->re_endp - pattern;
	} else
		len = strlen((char *)pattern);

	/* do the mallocs early so failure handling is easy */
	g = (struct re_guts *)malloc(sizeof(struct re_guts) +
							(NC-1)*sizeof(cat_t));
	if (g == NULL)
		return(REG_ESPACE);
	p->ssize = len/(size_t)2*(size_t)3 + (size_t)1;	/* ugh */
	p->strip = (sop *)malloc(p->ssize * sizeof(sop));
	p->slen = 0;
	if (p->strip == NULL) {
		free((char *)g);
		return(REG_ESPACE);
	}

	/* set things up */
	p->g = g;
	p->next = (char *)pattern;	/* convenience; we do not modify it */
	p->end = p->next + len;
	p->error = 0;
	p->ncsalloc = 0;
	for (i = 0; i < NPAREN; i++) {
		p->pbegin[i] = 0;
		p->pend[i] = 0;
	}
	g->csetsize = NC;
	g->sets = NULL;
	g->setbits = NULL;
	g->ncsets = 0;
	g->cflags = cflags;
	g->iflags = 0;
	g->nbol = 0;
	g->neol = 0;
	g->must = NULL;
	g->mlen = 0;
	g->nsub = 0;
	g->ncategories = 1;	/* category 0 is "everything else" */
	g->categories = &g->catspace[-(CHAR_MIN)];
	(void) memset((char *)g->catspace, 0, NC*sizeof(cat_t));
	g->backrefs = 0;

	/* do it */
	EMIT(OEND, 0);
	g->firststate = THERE();
	if (cflags&REG_EXTENDED)
		p_ere(p, OUT);
	else if (cflags&REG_NOSPEC)
		p_str(p);
	else
		p_bre(p, OUT, OUT);
	EMIT(OEND, 0);
	g->laststate = THERE();

	/* tidy up loose ends and fill things in */
	categorize(p, g);
	stripsnug(p, g);
	findmust(p, g);
	g->nplus = pluscount(p, g);
	g->magic = MAGIC2;
	preg->re_nsub = g->nsub;
	preg->re_g = g;
	preg->re_magic = MAGIC1;
#ifndef REDEBUG
	/* not debugging, so can't rely on the assert() in regexec() */
	if (g->iflags&BAD)
		SETERROR(REG_ASSERT);
#endif

	/* win or lose, we're done */
	if (p->error != 0)	/* lose */
		regfree(preg);
	return(p->error);
}

//
// regfree.c
//

/*
 - regfree - free everything
 = API_EXPORT(void) regfree(regex_t *);
 */
API_EXPORT(void)
regfree(regex_t *preg)
{
	register struct re_guts *g;

	if (preg->re_magic != MAGIC1)	/* oops */
		return;			/* nice to complain, but hard */

	g = preg->re_g;
	if (g == NULL || g->magic != MAGIC2)	/* oops again */
		return;
	preg->re_magic = 0;		/* mark it invalid */
	g->magic = 0;			/* mark it invalid */

	if (g->strip != NULL)
		free((char *)g->strip);
	if (g->sets != NULL)
		free((char *)g->sets);
	if (g->setbits != NULL)
		free((char *)g->setbits);
	if (g->must != NULL)
		free(g->must);
	free((char *)g);
}

//
// regexec.c
//

static int nope = 0;		/* for use in asserts; shuts lint up */

/* macros for manipulating states, small version */
#define	states	unsigned
#define	states1	unsigned	/* for later use in regexec() decision */
#define	CLEAR(v)	((v) = 0)
#define	SET0(v, n)	((v) &= ~((unsigned)1 << (n)))
#define	SET1(v, n)	((v) |= (unsigned)1 << (n))
#define	ISSET(v, n)	((v) & ((unsigned)1 << (n)))
#define	ASSIGN(d, s)	((d) = (s))
#define	EQ(a, b)	((a) == (b))
#define	STATEVARS	int dummy	/* dummy version */
#define	STATESETUP(m, n)	/* nothing */
#define	STATETEARDOWN(m)	/* nothing */
#define	SETUP(v)	((v) = 0)
#define	onestate	unsigned
#define	INIT(o, n)	((o) = (unsigned)1 << (n))
#define	INC(o)	((o) <<= 1)
#define	ISSTATEIN(v, o)	((v) & (o))
/* some abbreviations; note that some of these know variable names! */
/* do "if I'm here, I can also be there" etc without branches */
#define	FWD(dst, src, n)	((dst) |= ((unsigned)(src)&(here)) << (n))
#define	BACK(dst, src, n)	((dst) |= ((unsigned)(src)&(here)) >> (n))
#define	ISSETBACK(v, n)	((v) & ((unsigned)here >> (n)))
/* function names */
#define SNAMES			/* engine.c looks after details */

#include "RegExpressionEngine.cpp"

/* now undo things */
#undef	states
#undef	CLEAR
#undef	SET0
#undef	SET1
#undef	ISSET
#undef	ASSIGN
#undef	EQ
#undef	STATEVARS
#undef	STATESETUP
#undef	STATETEARDOWN
#undef	SETUP
#undef	onestate
#undef	INIT
#undef	INC
#undef	ISSTATEIN
#undef	FWD
#undef	BACK
#undef	ISSETBACK
#undef	SNAMES

/* macros for manipulating states, large version */
#define	states	char *
#define	CLEAR(v)	memset(v, 0, m->g->nstates)
#define	SET0(v, n)	((v)[n] = 0)
#define	SET1(v, n)	((v)[n] = 1)
#define	ISSET(v, n)	((v)[n])
#define	ASSIGN(d, s)	memcpy(d, s, m->g->nstates)
#define	EQ(a, b)	(memcmp(a, b, m->g->nstates) == 0)
#define	STATEVARS	int vn; char *space
#define	STATESETUP(m, nv)	{ (m)->space = (char *)malloc((nv)*(m)->g->nstates); \
				if ((m)->space == NULL) return(REG_ESPACE); \
				(m)->vn = 0; }
#define	STATETEARDOWN(m)	{ free((m)->space); }
#define	SETUP(v)	((v) = &m->space[m->vn++ * m->g->nstates])
#define	onestate	int
#define	INIT(o, n)	((o) = (n))
#define	INC(o)	((o)++)
#define	ISSTATEIN(v, o)	((v)[o])
/* some abbreviations; note that some of these know variable names! */
/* do "if I'm here, I can also be there" etc without branches */
#define	FWD(dst, src, n)	((dst)[here+(n)] |= (src)[here])
#define	BACK(dst, src, n)	((dst)[here-(n)] |= (src)[here])
#define	ISSETBACK(v, n)	((v)[here - (n)])
/* function names */
#define	LNAMES			/* flag */

#include "RegExpressionEngine.cpp"

/*
 - regexec - interface for matching
 = API_EXPORT(int) regexec(const regex_t *, const char *, size_t, \
 =					regmatch_t [], int);
 = #define	REG_NOTBOL	00001
 = #define	REG_NOTEOL	00002
 = #define	REG_STARTEND	00004
 = #define	REG_TRACE	00400	// tracing of execution
 = #define	REG_LARGE	01000	// force large representation
 = #define	REG_BACKR	02000	// force use of backref code
 *
 * We put this here so we can exploit knowledge of the state representation
 * when choosing which matcher to call.  Also, by this point the matchers
 * have been prototyped.
 */
API_EXPORT(int)				/* 0 success, REG_NOMATCH failure */
regexec(const regex_t *preg, const char *string, size_t nmatch, regmatch_t pmatch[], int eflags)
{
	register struct re_guts *g = preg->re_g;
#ifdef REDEBUG
#	undef	GOODFLAGS
#	define	GOODFLAGS(f)	(f)
#else
#	undef	GOODFLAGS
#	define	GOODFLAGS(f)	((f)&(REG_NOTBOL|REG_NOTEOL|REG_STARTEND))
#endif

	if (preg->re_magic != MAGIC1 || g->magic != MAGIC2)
		return(REG_BADPAT);
	assert(!(g->iflags&BAD));
	if (g->iflags&BAD)		/* backstop for no-debug case */
		return(REG_BADPAT);
	eflags = GOODFLAGS(eflags);

	if (g->nstates <= CHAR_BIT*sizeof(states1) && !(eflags&REG_LARGE))
		return(smatcher(g, (char *)string, nmatch, pmatch, eflags));
	else
		return(lmatcher(g, (char *)string, nmatch, pmatch, eflags));
}
