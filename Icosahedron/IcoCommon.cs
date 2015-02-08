﻿using System;
using System.Collections.Generic;

namespace Icosahedron
{
    public static class IcoCommon
    {
        internal static Vector3d[] verticies;
        internal static Face[][] faces;
        internal static int[][] neighbours;
        private static int calculateLevel = -1;
        private static int calculateLevelNeighbours = -1;

        public static void Precalculate(int level)
        {
            while (calculateLevel < level)
            {
                calculateLevel++;
                if (calculateLevel == 0)
                {
                    verticies = new Vector3d[1];
                    faces = new Face[1][];
                    neighbours = new int[1][];
                    verticies = GetInitialVerticies();
                    faces[0] = GetInitialFaces();
                    CalculateNeighbours(0);
                }
                else
                {
                    Face[][] oldFaces = faces;
                    faces = new Face[calculateLevel + 1][];
                    Array.Copy(oldFaces, 0, faces, 0, oldFaces.Length);
                    Subdivide(calculateLevel);
                }
            }
        }

        public static void PrecalculateNeighbours(int level)
        {
            while (calculateLevelNeighbours < level)
            {
                Precalculate(level);
                int[][] oldNeighbours = neighbours;
                neighbours = new int[calculateLevel + 1][];
                for (int i = 0; i < oldNeighbours.Length; i++)
                {
                    neighbours[i] = oldNeighbours[i];
                }
                CalculateNeighbours(calculateLevel);
                calculateLevelNeighbours++;
            }
        }

        public static Vector3d[] GetVerticesCopy(int level)
        {
            Precalculate(level);
            Vector3d[] retArray = new Vector3d[VerticiesInLevel(level)];
            Array.Copy(verticies, 0, retArray, 0, retArray.Length);
            return retArray;
        }

        public static Face[] GetFacesCopy(int level)
        {
            Precalculate(level);
            Face[] retArray = new Face[faces[level].Length];
            Array.Copy(faces[level], 0, retArray, 0, retArray.Length);
            return retArray;
        }

        public static int[] GetNeighboursCopy(int level)
        {
            PrecalculateNeighbours(level);
            int[] retArray = new int[neighbours[level].Length];
            Array.Copy(neighbours[level], 0, retArray, 0, retArray.Length);
            return retArray;
        }

        public static int[] GetNeighbours(int level)
        {
            PrecalculateNeighbours(level);
            return neighbours[level];
        }

        public static long VerticiesInLevel(int level)
        {
            return 10 * ipow(4, level) + 2;
        }

        public static long FacesInLevel(int level)
        {
            return 20 * ipow(4, level);
        }

        public static long EdgesInLevel(int level)
        {
            return 30 * ipow(4, level);
        }

        private static long ipow(long baseVal, long exp)
        {
            long result = 1;
            while (exp > 0)
            {
                if ((exp & 1) == 1)
                {
                    result *= baseVal;
                }
                exp >>= 1;
                baseVal *= baseVal;
            }
            return result;
        }

        public static int Raycast(Vector3d direction, int level, bool normalize)
        {
            return Raycast(verticies, direction, level, normalize);
        }

        public static int Raycast(Vector3d[] raycastVerticies, Vector3d direction, int level, bool normalize)
        {
            if (normalize)
            {
                direction = direction.normalized;
            }
            Precalculate(level);
            PrecalculateNeighbours(level);
            int currentVertexID = 0;
            Vector3d currentVertex = raycastVerticies[currentVertexID];
            double currentDot = Vector3d.Dot(direction, currentVertex);
            if (normalize)
            {
                currentVertex = currentVertex.normalized;
            }
            //Search level
            for (int searchLevel = 0; searchLevel <= level; searchLevel++)
            {
                int[] currentNeighbours = neighbours[searchLevel];
                //Walking state
                int newVertexID = currentVertexID;
                Vector3d newVertex = currentVertex;
                double newDot = currentDot;
                bool searchComplete = false;
                while (!searchComplete)
                {
                    for (int searchNeighbour = 0; searchNeighbour < 6; searchNeighbour++)
                    {
                        int neighbourID = currentNeighbours[currentVertexID * 6 + searchNeighbour];
                        if (neighbourID == -1)
                        {
                            break;
                        }
                        if (neighbourID == currentVertexID)
                        {
                            continue;
                        }
                        //Compare Dot product, bigger == closer.
                        Vector3d neighbourVertex = verticies[neighbourID];
                        if (normalize)
                        {
                            neighbourVertex = neighbourVertex.normalized;
                        }
                        double neighbourDot = Vector3d.Dot(direction, neighbourVertex);
                        if (neighbourDot > newDot)
                        {
                            newVertex = neighbourVertex;
                            newVertexID = neighbourID;
                            newDot = neighbourDot;
                        }
                    }
                    //Set next search state
                    if (currentVertexID == newVertexID)
                    {
                        searchComplete = true;
                    }
                    else
                    {
                        currentVertex = newVertex;
                        currentVertexID = newVertexID;
                        currentDot = newDot;
                    }
                }
            }
            return currentVertexID;
        }

        private static void Subdivide(int level)
        {
            List<Vector3d> newVertex = new List<Vector3d>(verticies);
            List<Face> newFaces = new List<Face>();
            Dictionary<ulong, int> meshCache = new Dictionary<ulong, int>();
            Face[] oldLevel = faces[level - 1];
            for (int faceIndex = 0; faceIndex < oldLevel.Length; faceIndex++)
            {
                Face face = oldLevel[faceIndex];
                Vector3d point1Vector = verticies[face.point1];
                Vector3d point2Vector = verticies[face.point2];
                Vector3d point3Vector = verticies[face.point3];
                //Too slow to be useful
                /*
                CachePair cacheID12;
                CachePair cacheID13;
                CachePair cacheID23;
                if (face.point1 < face.point2)
                {
                    cacheID12 = new CachePair(face.point1, face.point2);
                }
                else
                {
                    cacheID12 = new CachePair(face.point2, face.point1);
                }
                if (face.point1 < face.point3)
                {
                    cacheID13 = new CachePair(face.point1, face.point3);
                }
                else
                {
                    cacheID13 = new CachePair(face.point3, face.point1);
                }
                if (face.point2 < face.point3)
                {
                    cacheID23 = new CachePair(face.point2, face.point3);
                }
                else
                {
                    cacheID23 = new CachePair(face.point3, face.point2);
                }
                */
                ulong cacheID12;
                ulong cacheID13;
                ulong cacheID23;
                //Hash code problem workaround
                if (face.point1 < face.point2)
                {
                    uint bigPoint = (uint)face.point2;
                    cacheID12 = (ulong)face.point1 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                else
                {
                    uint bigPoint = (uint)face.point1;
                    cacheID12 = (ulong)face.point2 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                if (face.point1 < face.point3)
                {
                    uint bigPoint = (uint)face.point3;
                    cacheID13 = (ulong)face.point1 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                else
                {
                    uint bigPoint = (uint)face.point1;
                    cacheID13 = (ulong)face.point3 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                if (face.point2 < face.point3)
                {
                    uint bigPoint = (uint)face.point3;
                    cacheID23 = (ulong)face.point2 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                else
                {
                    uint bigPoint = (uint)face.point2;
                    cacheID23 = (ulong)face.point3 << 32 | (bigPoint & 0xFFFF0000) >> 16 | (bigPoint & 0xFFFF) << 16;
                }
                int newPoint1;
                int newPoint2;
                int newPoint3;
                //Add or select the vertex between point 12
                if (!meshCache.TryGetValue(cacheID12, out newPoint1))
                {
                    newPoint1 = newVertex.Count;
                    newVertex.Add((point1Vector + point2Vector).normalized);
                    meshCache.Add(cacheID12, newPoint1);
                }

                //Add or select the vertex between point 13
                if (!meshCache.TryGetValue(cacheID13, out newPoint2))
                {
                    newPoint2 = newVertex.Count;
                    newVertex.Add((point1Vector + point3Vector).normalized);
                    meshCache.Add(cacheID13, newPoint2);
                }

                //Add or select the vertex between point 23
                if (!meshCache.TryGetValue(cacheID23, out newPoint3))
                {
                    newPoint3 = newVertex.Count;
                    newVertex.Add((point2Vector + point3Vector).normalized);
                    meshCache.Add(cacheID23, newPoint3);
                }
                //Add the faces
                newFaces.Add(new Face(face.point1, newPoint1, newPoint2));
                newFaces.Add(new Face(face.point2, newPoint3, newPoint1));
                newFaces.Add(new Face(face.point3, newPoint2, newPoint3));
                newFaces.Add(new Face(newPoint3, newPoint2, newPoint1));
            }
            verticies = newVertex.ToArray();
            faces[level] = newFaces.ToArray();
        }

        private static Vector3d[] GetInitialVerticies()
        {
            //Definition:
            //2 points at the poles
            //10 points located around the equator 36 degrees apart, that alternate +/- atan(0.5) inclination
            Vector3d[] returnPoints = new Vector3d[12];
            //Poles
            returnPoints[0] = new Vector3d(0, 1, 0);
            returnPoints[11] = new Vector3d(0, -1, 0);
            //Points

            //Get equator inclination and z value offset
            double equatorInc = Math.PI / 2d - Math.Atan(0.5);
            double equatorOffset = Math.Cos(equatorInc);

            double SinInc = Math.Sin(equatorInc);
            double Cos36 = Math.Sin(equatorInc) * Math.Cos(Math.PI / 10d);
            double Cos72 = Math.Sin(equatorInc) * Math.Cos(Math.PI / 5d);
            double Sin36 = Math.Sin(equatorInc) * Math.Sin(Math.PI / 10d);
            double Sin72 = Math.Sin(equatorInc) * Math.Sin(Math.PI / 5d);
            //Build points
            returnPoints[1] = new Vector3d(SinInc, equatorOffset, 0);
            returnPoints[2] = new Vector3d(Cos72, -equatorOffset, Sin72);
            returnPoints[3] = new Vector3d(Sin36, equatorOffset, Cos36);
            returnPoints[4] = new Vector3d(-Sin36, -equatorOffset, Cos36);
            returnPoints[5] = new Vector3d(-Cos72, equatorOffset, Sin72);
            returnPoints[6] = new Vector3d(-SinInc, -equatorOffset, 0);
            returnPoints[7] = new Vector3d(-Cos72, equatorOffset, -Sin72);
            returnPoints[8] = new Vector3d(-Sin36, -equatorOffset, -Cos36);
            returnPoints[9] = new Vector3d(Sin36, equatorOffset, -Cos36);
            returnPoints[10] = new Vector3d(Cos72, -equatorOffset, -Sin72);
            return returnPoints;
        }

        private static Face[] GetInitialFaces()
        {
            Face[] returnFaces = new Face[20];
            //Point 0
            returnFaces[0] = new Face(0, 3, 1);
            returnFaces[1] = new Face(0, 5, 3);
            returnFaces[2] = new Face(0, 7, 5);
            returnFaces[3] = new Face(0, 9, 7);
            returnFaces[4] = new Face(0, 1, 9);

            //Middle
            returnFaces[5] = new Face(1, 3, 2);
            returnFaces[6] = new Face(2, 3, 4);
            returnFaces[7] = new Face(3, 5, 4);
            returnFaces[8] = new Face(4, 5, 6);
            returnFaces[9] = new Face(5, 7, 6);
            returnFaces[10] = new Face(6, 7, 8);
            returnFaces[11] = new Face(7, 9, 8);
            returnFaces[12] = new Face(8, 9, 10);
            returnFaces[13] = new Face(9, 1, 10);
            returnFaces[14] = new Face(10, 1, 2);

            //Point 11
            returnFaces[15] = new Face(11, 2, 4);
            returnFaces[16] = new Face(11, 4, 6);
            returnFaces[17] = new Face(11, 6, 8);
            returnFaces[18] = new Face(11, 8, 10);
            returnFaces[19] = new Face(11, 10, 2);

            return returnFaces;
        }

        private static void CalculateNeighbours(int level)
        {
            int maxVertex = 0;
            Face[] currentFaces = faces[level];
            foreach (Face f in currentFaces)
            {
                if (f.point1 > maxVertex)
                {
                    maxVertex = f.point1;
                }
                if (f.point2 > maxVertex)
                {
                    maxVertex = f.point2;
                }
                if (f.point3 > maxVertex)
                {
                    maxVertex = f.point3;
                }
            }
            int[] newNeighbours = new int[(maxVertex + 1) * 6];
            int[] storePos = new int[maxVertex + 1];
            for (int i = 0; i < newNeighbours.Length; i++)
            {
                newNeighbours[i] = -1;
            }
            for (int i = 0; i < currentFaces.Length; i++)
            {
                Face currentFace = currentFaces[i];
                LinkPoints(newNeighbours, storePos, currentFace.point1, currentFace.point2);
                LinkPoints(newNeighbours, storePos, currentFace.point1, currentFace.point3);
                LinkPoints(newNeighbours, storePos, currentFace.point2, currentFace.point1);
                LinkPoints(newNeighbours, storePos, currentFace.point2, currentFace.point3);
                LinkPoints(newNeighbours, storePos, currentFace.point3, currentFace.point1);
                LinkPoints(newNeighbours, storePos, currentFace.point3, currentFace.point2);
            }
            neighbours[level] = newNeighbours;
        }

        private static void LinkPoints(int[] neighbours, int[] storePos, int pointSrc, int pointDst)
        {
            bool pointLinked = false;
            for (int i = pointSrc * 6; i < pointSrc * 6 + 6; i++)
            {
                if (neighbours[i] == pointDst)
                {
                    pointLinked = true;
                    break;
                }
            }
            if (!pointLinked)
            {
                int storeIndex = storePos[pointSrc];
                neighbours[pointSrc * 6 + storeIndex] = pointDst;
                storePos[pointSrc] = storeIndex + 1;
            }
        }

        //Too slow to be useful
        /*
        private struct CachePair
        {
            public readonly int smallIndex;
            public readonly int bigIndex;

            public CachePair(int smallIndex, int bigIndex)
            {
                this.smallIndex = smallIndex;
                this.bigIndex = bigIndex;
            }

            public override bool Equals(object obj)
            {
                if (obj is CachePair)
                {
                    CachePair theirObject = (CachePair)obj;
                    return smallIndex == theirObject.smallIndex && bigIndex == theirObject.bigIndex;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return smallIndex ^ bigIndex;
            }
        }
        */
    }
}

