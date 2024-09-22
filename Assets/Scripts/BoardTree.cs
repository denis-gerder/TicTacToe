using System.Collections.Generic;

namespace Assets.Scripts
{
    public class BoardTree
    {

        public int[,] Data;
        private BoardTree _parent;
        private readonly LinkedList<BoardTree> _children;
        private string _stringRepresentation;
        public float Score = 111;

        private static int _idTracker = 0;
        private readonly int _id;


        public BoardTree()
        {
            this.Data = null;
            this._parent = null;
            _children = new LinkedList<BoardTree>();
            _id = _idTracker;
            _idTracker++;
        }

        public BoardTree(int[,] data)
        {
            this.Data = data;
            this._parent = null;
            _children = new LinkedList<BoardTree>();
            _id = _idTracker;
            _idTracker++;
        }

        public BoardTree AddChild(int[,] data)
        {
            BoardTree child = new(data);
            _children.AddLast(child);
            child.SetParent(this);

            return child;
        }

        public BoardTree GetChild(int i)
        {
            foreach (BoardTree childTree in _children)
                if (--i == 0)
                    return childTree;
            return null;
        }

        public BoardTree SetParent(BoardTree parent)
        {
            this._parent = parent;
            return this;
        }

        public BoardTree GetParent()
        {
            return _parent;
        }

        public string PrintTree()
        {
            _stringRepresentation = "";
            Dictionary<int, List<BoardTree>> nodesPerDepth = new();
            PutNodesForDepths(this, nodesPerDepth, 0);
            for (int depth = nodesPerDepth.Count - 1; depth >= 1; depth--)
            {
                nodesPerDepth.TryGetValue(depth, out List<BoardTree> list);
                string LastRows = _stringRepresentation;
                _stringRepresentation = PrintTreeAtDepth(list) + "\n" + LastRows;
            }
            return _stringRepresentation;
        }

        public string PrintTreeAtDepth(List<BoardTree> nodesAtDepth)
        {
            _stringRepresentation = "";
            for (int i = nodesAtDepth[0].Data.GetLength(0); i >= -1; i--)
            {

                BoardTree parent = new();
                foreach (BoardTree child in nodesAtDepth)
                {
                    if (parent != child.GetParent())
                    {
                        _stringRepresentation += "|      ";
                    }
                    parent = child.GetParent();

                    if (i == nodesAtDepth[0].Data.GetLength(0))
                    {
                        string idString = child._id + ":" + parent._id;
                        int stringLengthPerBoard = nodesAtDepth[0].Data.GetLength(0) * 2 + 5;

                        _stringRepresentation += idString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(idString.Length * 1.2); j++)
                        {
                            _stringRepresentation += " ";
                        }
                    }
                    else if (i == -1)
                    {
                        string scoreString = "Sc: " + child.Score;
                        int stringLengthPerBoard = nodesAtDepth[0].Data.GetLength(0) * 2 + 5;
                        if (child._children.Count == 0)
                        {
                            scoreString += "T";
                        }

                        _stringRepresentation += scoreString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(scoreString.Length * 1.17); j++)
                        {
                            _stringRepresentation += " ";
                        }
                    }
                    else
                    {
                        for (int j = 0; j < child.Data.GetLength(1); j++)
                        {
                            _stringRepresentation += child.Data[i, j] + " ";
                        }
                        _stringRepresentation += "     ";
                    }

                }
                _stringRepresentation += "\n";
            }

            return _stringRepresentation;
        }

        private void PutNodesForDepths(BoardTree node, Dictionary<int, List<BoardTree>> nodesPerDepth, int depth)
        {
            if (!nodesPerDepth.ContainsKey(depth))
            {
                nodesPerDepth.Add(depth, new List<BoardTree>());
            }
            nodesPerDepth[depth].Add(node);
            foreach (BoardTree child in node._children)
            {
                PutNodesForDepths(child, nodesPerDepth, depth + 1);
            }
        }
    }
}
