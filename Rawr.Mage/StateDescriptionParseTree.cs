// Generated by TinyPG v1.2 available at www.codeproject.com

using System;
using System.Collections.Generic;
using System.Text;

namespace Rawr.Mage.StateDescription
{
    #region ParseTree
    public class ParseErrors : List<ParseError>
    {
    }

    public class ParseError
    {
        private string message;
        private int code;
        private int line;
        private int col;
        private int pos;
        private int length;

        public int Code { get { return code; } }
        public int Line { get { return line; } }
        public int Column { get { return col; } }
        public int Position { get { return pos; } }
        public int Length { get { return length; } }
        public string Message { get { return message; } }

        public ParseError(string message, int code, ParseNode node) : this(message, code,  0, node.Token.StartPos, node.Token.StartPos, node.Token.Length)
        {
        }

        public ParseError(string message, int code, int line, int col, int pos, int length)
        {
            this.message = message;
            this.code = code;
            this.line = line;
            this.col = col;
            this.pos = pos;
            this.length = length;
        }
    }

    public delegate bool StateDescriptionDelegate(int effects);

    // rootlevel of the node tree
    public partial class ParseTree : ParseNode
    {
        public ParseErrors Errors;

        public List<Token> Skipped;

        public ParseTree() : base(new Token(), "ParseTree")
        {
            Token.Type = TokenType.Start;
            Token.Text = "Root";
            Skipped = new List<Token>();
            Errors = new ParseErrors();
        }

        public string PrintTree()
        {
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            PrintNode(sb, this, indent);
            return sb.ToString();
        }

        private void PrintNode(StringBuilder sb, ParseNode node, int indent)
        {
            
            string space = "".PadLeft(indent, ' ');

            sb.Append(space);
            sb.AppendLine(node.Text);

            foreach (ParseNode n in node.Nodes)
                PrintNode(sb, n, indent + 2);
        }
        
        /// <summary>
        /// this is the entry point for executing and evaluating the parse tree.
        /// </summary>
        /// <param name="paramlist">additional optional input parameters</param>
        /// <returns>the output of the evaluation function</returns>
        public StateDescriptionDelegate Compile()
        {
            return Nodes[0].Compile(this);
        }

        public bool Interpret(int effects)
        {
            return Nodes[0].Interpret(this, effects);
        }
    }

    public partial class ParseNode
    {
        protected string text;
        protected List<ParseNode> nodes;
        
        public List<ParseNode> Nodes { get {return nodes;} }
        
        public ParseNode Parent;
        public Token Token; // the token/rule
        public string Text { // text to display in parse tree 
            get { return text;} 
            set { text = value; }
        } 

        public virtual ParseNode CreateNode(Token token, string text)
        {
            ParseNode node = new ParseNode(token, text);
            node.Parent = this;
            return node;
        }

        protected ParseNode(Token token, string text)
        {
            this.Token = token;
            this.text = text;
            this.nodes = new List<ParseNode>();
        }

        internal StateDescriptionDelegate Compile(ParseTree tree)
        {
            StateDescriptionDelegate Value = null;

            switch (Token.Type)
            {
                case TokenType.Start:
                    Value = EvalStart(tree);
                    break;
                case TokenType.UnionExpr:
                    Value = EvalUnionExpr(tree);
                    break;
                case TokenType.DiffExpr:
                    Value = EvalDiffExpr(tree);
                    break;
                case TokenType.IntExpr:
                    Value = EvalIntExpr(tree);
                    break;
                case TokenType.CompExpr:
                    Value = EvalCompExpr(tree);
                    break;
                case TokenType.Atom:
                    Value = EvalAtom(tree);
                    break;

                default:
                    Value = delegate { return true; };
                    break;
            }
            return Value;
        }

        internal bool Interpret(ParseTree tree, int effects)
        {
            bool Value = true;

            switch (Token.Type)
            {
                case TokenType.Start:
                    Value = EvalStart(tree, effects);
                    break;
                case TokenType.UnionExpr:
                    Value = EvalUnionExpr(tree, effects);
                    break;
                case TokenType.DiffExpr:
                    Value = EvalDiffExpr(tree, effects);
                    break;
                case TokenType.IntExpr:
                    Value = EvalIntExpr(tree, effects);
                    break;
                case TokenType.CompExpr:
                    Value = EvalCompExpr(tree, effects);
                    break;
                case TokenType.Atom:
                    Value = EvalAtom(tree, effects);
                    break;

                default:
                    Value = true;
                    break;
            }
            return Value;
        }

        protected virtual bool EvalStart(ParseTree tree, int effects)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.UnionExpr)
                {
                    return node.Interpret(tree, effects);
                }
            }
            return true;
        }

        protected virtual bool EvalUnionExpr(ParseTree tree, int effects)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.DiffExpr)
                {
                    bool value = node.Interpret(tree, effects);
                    if (value) return true;
                }
            }
        	return false;
        }

        protected virtual bool EvalDiffExpr(ParseTree tree, int effects)
        {
            int i = 0;
            for (; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.IntExpr)
                {
                    bool value = node.Interpret(tree, effects);
                    if (!value) return false;
                    i++;
                    break;
                }
            }
            for (; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.IntExpr)
                {
                    return !node.Interpret(tree, effects);
                }
            }
            return true;
        }

        protected virtual bool EvalIntExpr(ParseTree tree, int effects)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.CompExpr)
                {
                    bool value = node.Interpret(tree, effects);
                    if (!value) return false;
                }
            }
            return true;
        }

        protected virtual bool EvalCompExpr(ParseTree tree, int effects)
        {
            bool complement = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.COMPLEMENT)
                {
                    complement = true;
                }
                else if (node.Token.Type == TokenType.Atom)
                {
                    return node.Interpret(tree, effects) ^ complement;
                }
            }
            return true;
        }

        private int EvalState(ParseNode node)
        {
            string text = node.Token.Text.Replace(" ", "");
            if (text.ToUpper() == "ANY")
            {
                return 0;
            }
            else
            {
                return (int)Enum.Parse(typeof(StandardEffect), text, true);
            }
        }

        protected virtual bool EvalAtom(ParseTree tree, int effects)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.UnionExpr)
                {
                    return node.Interpret(tree, effects);
                }
                else if (node.Token.Type == TokenType.STATE)
                {
                    int stateEffects = EvalState(node);
                    return (effects & stateEffects) == stateEffects;
                }
            }
            return true;
        }

        protected virtual StateDescriptionDelegate EvalStart(ParseTree tree)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.UnionExpr)
                {
                    return node.Compile(tree);
                }
            }
            return delegate { return true; };
        }

        protected virtual StateDescriptionDelegate EvalUnionExpr(ParseTree tree)
        {
            List<StateDescriptionDelegate> nodelist = new List<StateDescriptionDelegate>();
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.DiffExpr)
                {
                    nodelist.Add(node.Compile(tree));
                }
            }
            StateDescriptionDelegate[] list = nodelist.ToArray();
            if (list.Length == 1) return list[0];
            return delegate(int effects)
            {
                for (int i = 0; i < list.Length; i++)
                {                    
                    if (list[i](effects)) return true;
                }
                return false;
            };
        }

        protected virtual StateDescriptionDelegate EvalDiffExpr(ParseTree tree)
        {
            StateDescriptionDelegate a = null;
            StateDescriptionDelegate b = null;
            int i = 0;
            for (; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.IntExpr)
                {
                    a = node.Compile(tree);
                    i++;
                    break;
                }
            }
            for (; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.IntExpr)
                {
                    b = node.Compile(tree);
                    break;
                }
            }
            if (b != null)
            {
                return delegate(int effects)
                {
                    return a(effects) && !b(effects);
                };
            }
            else
            {
                return a;
            }
        }

        protected virtual StateDescriptionDelegate EvalIntExpr(ParseTree tree)
        {
            bool allAtom = true;
            List<StateDescriptionDelegate> nodelist = new List<StateDescriptionDelegate>();
            int stateEffects = 0;
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.CompExpr)
                {
                    if (node.nodes[0].Token.Type == TokenType.Atom && node.nodes[0].nodes[0].Token.Type == TokenType.STATE)
                    {
                        stateEffects = stateEffects | EvalState(node.nodes[0].nodes[0]);
                    }
                    else
                    {
                        allAtom = false;
                    }
                    nodelist.Add(node.Compile(tree));
                }
            }
            if (allAtom)
            {
                return delegate(int effects)
                {
                    return (effects & stateEffects) == stateEffects;
                };
            }
            else
            {
                StateDescriptionDelegate[] list = nodelist.ToArray();
                if (list.Length == 1) return list[0];
                return delegate(int effects)
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        if (!list[i](effects)) return false;
                    }
                    return true;
                };
            }
        }

        protected virtual StateDescriptionDelegate EvalCompExpr(ParseTree tree)
        {
            bool complement = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                ParseNode node = nodes[i];
                if (node.Token.Type == TokenType.COMPLEMENT)
                {
                    complement = true;
                }
                else if (node.Token.Type == TokenType.Atom)
                {
                    StateDescriptionDelegate value = node.Compile(tree);
                    if (complement)
                    {
                        return delegate(int effects)
                        {
                            return !value(effects);
                        };
                    }
                    else
                    {
                        return value;
                    }
                }
            }
            return null;
        }

        protected virtual StateDescriptionDelegate EvalAtom(ParseTree tree)
        {
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == TokenType.UnionExpr)
                {
                    return node.Compile(tree);
                }
                else if (node.Token.Type == TokenType.STATE)
                {
                    int stateEffects = EvalState(node);
                    return delegate(int effects)
                    {
                        return (effects & stateEffects) == stateEffects;
                    };
                }
            }
            return null;
        }
    }
    
    #endregion ParseTree
}
