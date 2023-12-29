using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class UnionFind
    {
        private int[] parent;
        private int[] rank;

        // Constructor to initialize the UnionFind data structure
        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];

            // Initially, each element is its own parent (self-root)
            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        // Find operation with path compression
        public int Find(int x)
        {
            if (parent[x] != x)
            {
                // Path compression: update the parent to the root
                parent[x] = Find(parent[x]);
            }
            return parent[x];
        }

        // Union operation with union by rank
        public void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return; // Already in the same set

            // Merge smaller tree into larger tree based on rank
            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else if (rank[rootY] < rank[rootX])
            {
                parent[rootY] = rootX;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX] += 1; // Increase the rank if both have the same rank
            }
        }
    }

}
