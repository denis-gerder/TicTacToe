using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

namespace Assets.Scripts
{
    public class BoardTree
    {

        public int[,] data;
        private BoardTree parent;
        private LinkedList<BoardTree> children;
        private String StringRepresentation;
        public int score = 1000;
        public int alpha = 1000;
        public int beta = 1000;

        private static int idTracker = 0;

        private int id;


        public BoardTree()
        {
            this.data = null;
            this.parent = null;
            children = new LinkedList<BoardTree>();
            id = idTracker;
            idTracker++;
        }

        public BoardTree(int[,] data)
        {
            this.data = data;
            this.parent = null;
            children = new LinkedList<BoardTree>();
            id = idTracker;
            idTracker++;
        }

        public BoardTree AddChild(int[,] data)
        {
            BoardTree child = new BoardTree(data);
            children.AddLast(child);
            child.SetParent(this);

            return child;
        }

        public BoardTree GetChild(int i)
        {
            foreach (BoardTree childTree in children)
                if (--i == 0)
                    return childTree;
            return null;
        }

        public BoardTree SetParent(BoardTree parent)
        {
            this.parent = parent;
            return this;
        }

        public BoardTree GetParent()
        {
            return parent;
        }

        public String PrintTree()
        {
            StringRepresentation = "";
            Dictionary<int, List<BoardTree>> nodesPerDepth = new Dictionary<int, List<BoardTree>>();
            PutNodesForDepths(this, nodesPerDepth, 0);
            for (int depth = nodesPerDepth.Count - 1; depth >= 1; depth--)
            {
                nodesPerDepth.TryGetValue(depth, out List<BoardTree> list);
                String LastRows = StringRepresentation;
                StringRepresentation = PrintTreeAtDepth(list) + "\n" + LastRows;
            }
            return StringRepresentation;
        }

        public String PrintTreeAtDepth(List<BoardTree> nodesAtDepth)
        {
            StringRepresentation = "";
            for (int i = nodesAtDepth[0].data.GetLength(0); i >= -3; i--)
            {

                BoardTree parent = new BoardTree();
                foreach (BoardTree child in nodesAtDepth)
                {
                    if (parent != child.GetParent())
                    {
                        StringRepresentation += "|      ";
                    }
                    parent = child.GetParent();

                    if (i == nodesAtDepth[0].data.GetLength(0))
                    {
                        String idString = child.id + ":" + parent.id;
                        int stringLengthPerBoard = nodesAtDepth[0].data.GetLength(0) * 2 + 5;

                        StringRepresentation += idString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(idString.Length * 1.2); j++)
                        {
                            StringRepresentation += " ";
                        }
                    }
                    else if (i == -1)
                    {
                        String scoreString = "Sc: " + child.score;
                        int stringLengthPerBoard = nodesAtDepth[0].data.GetLength(0) * 2 + 5;
                        if (child.children.Count == 0)
                        {
                            scoreString += "T";
                        }

                        StringRepresentation += scoreString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(scoreString.Length * 1.17); j++)
                        {
                            StringRepresentation += " ";
                        }
                    }
                    else if (i == -2)
                    {
                        String scoreString = "a: " + child.alpha;
                        int stringLengthPerBoard = nodesAtDepth[0].data.GetLength(0) * 2 + 5;

                        StringRepresentation += scoreString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(scoreString.Length * 1.17); j++)
                        {
                            StringRepresentation += " ";
                        }
                    }
                    else if (i == -3)
                    {
                        String scoreString = "b: " + child.beta;
                        int stringLengthPerBoard = nodesAtDepth[0].data.GetLength(0) * 2 + 5;

                        StringRepresentation += scoreString;

                        for (int j = 0; j < stringLengthPerBoard - (int)(scoreString.Length * 1.17); j++)
                        {
                            StringRepresentation += " ";
                        }
                    }
                    else
                    {
                        for (int j = 0; j < child.data.GetLength(1); j++)
                        {
                            StringRepresentation += child.data[i, j] + " ";
                        }
                        StringRepresentation += "     ";
                    }

                }
                StringRepresentation += "\n";
            }

            return StringRepresentation;
        }

        private void PutNodesForDepths(BoardTree node, Dictionary<int, List<BoardTree>> nodesPerDepth, int depth)
        {
            if (!nodesPerDepth.ContainsKey(depth))
            {
                nodesPerDepth.Add(depth, new List<BoardTree>());
            }
            nodesPerDepth[depth].Add(node);
            foreach (BoardTree child in node.children)
            {
                PutNodesForDepths(child, nodesPerDepth, depth + 1);
            }
        }
    }
}
