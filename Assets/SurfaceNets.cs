using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * SurfaceNets based isosurface extraction.
 *
 * Based on the slightly scary-looking, compact and efficient JavaScript
 * implementation by Mikola Lysenko. 
 *
 * In addition this implementation comes with simple CSG support (union,
 * difference and intersection). Meshes can be saved as *.obj-File.
 *
 * Based on: S.F. Gibson, "Constrained Elastic Surface Nets". (1998) MERL Tech
 * Report.
 *
 * The only difference to the method described in the paper is that this
 * algorithms chooses the average point of the edge-crossing points on the
 * surface as cell coordinate for the quad-mesh generation.
 * 
 * This works surprisingly well! For many cases this algorithm is a good
 * substitute for the marching cubes algorithm. This implementation can also
 * be used to create more intelligent dual methods by replacing the
 * "average point" code.
 *
 * @author Michael Hoffer &lt;info@michaelhoffer.de&gt;
 */
public class SurfaceNets {
	
	private static readonly int[] cube_edges = new int[24];
	private static readonly int[] edge_table = new int[256];
	private static int[] vertexBuffer = new int[4096];
	
	/**
     * Initializes the cube edge indices. This follows the idea of Paul Bourke
     * link: http://paulbourke.net/geometry/polygonise/
     */
	static void initCubeEdges() {
		int k = 0;
		for (int i = 0; i < 8; ++i) {
			for (int j = 1; j <= 4; j <<= 1) {
				int p = i ^ j;
				if (i <= p) {
					cube_edges[k++] = i;
					cube_edges[k++] = p;
				}
			}
		}
	}
	
	/**
     * Initializes the cube edge table. This follows the idea of Paul Bourke
     * link: http://paulbourke.net/geometry/polygonise/
     */
	static void initEdgeTable() {
		for (int i = 0; i < 256; ++i) {
			int em = 0;
			for (int j = 0; j < 24; j += 2) {
				bool a = boolean(i & (1 << cube_edges[j]));
				bool b = boolean(i & (1 << cube_edges[j + 1]));
				em |= a != b ? (1 << (j >> 1)) : 0;
			}
			edge_table[i] = em;
		}
	}
	
	static SurfaceNets() {
		initCubeEdges();
		initEdgeTable();
	}
	
	/**
     * Minimal mesh class.
     */
	public sealed class Mesh {
		
		public readonly Vector3[] vertices;
        public readonly int[] triangles;

        public Mesh(Vector3[] vertices, int[] triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
	}
	
	/**
     * Runs the surface nets algorithm.
     *
     * @param data iso data
     * @param dims dimensions
     * @return mesh
     */
	public static Mesh Run(float[] data, int[] dims) {
		
		// location (location[0]=x, location[1]=y, location[2]=z)
		int[] location = new int[3];
		// layout for one-dimensional data array
		// we use this to reference vertex buffer
		int[] R = {
			// x
			1,
			// y * width
			dims[0] + 1,
			// z * width * height
			(dims[0] + 1) * (dims[1] + 1)
		};
		// grid cell
		float[] grid = new float[8];
		
		// TODO: is is the only mystery that is left
		int buf_no = 1;
		
		
		// Resize buffer if necessary 
		if (R[2] * 2 > vertexBuffer.Length) {
			vertexBuffer = new int[R[2] * 2];
		}
		
		// we make some assumptions about the number of vertices and faces
		// to reduce GC overhead
		List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
		
		int n = 0;
		
		// March over the voxel grid
		for (location[2] = 0; location[2] < dims[2] - 1; ++location[2], n += dims[0], buf_no ^= 1 /*even or odd*/, R[2] = -R[2]) {

			// m is the pointer into the buffer we are going to use.  
			// This is slightly obtuse because javascript does not
			// have good support for packed data structures, so we must
			// use typed arrays :(
			// The contents of the buffer will be the indices of the
			// vertices on the previous x/y slice of the volume
			int m = 1 + (dims[0] + 1) * (1 + buf_no * (dims[1] + 1));
			
			for (location[1] = 0; location[1] < dims[1] - 1; ++location[1], ++n, m += 2) {
				for (location[0] = 0; location[0] < dims[0] - 1; ++location[0], ++n, ++m) {
					
					// Read in 8 field values around this vertex
					// and store them in an array
					// Also calculate 8-bit mask, like in marching cubes,
					// so we can speed up sign checks later
					int mask = 0, g = 0, idx = n;
					for (int k = 0; k < 2; ++k, idx += dims[0] * (dims[1] - 2)) {
						for (int j = 0; j < 2; ++j, idx += dims[0] - 2) {
							for (int i = 0; i < 2; ++i, ++g, ++idx) {
								float p = data[idx];
								grid[g] = p;
								mask |= (p < 0) ? (1 << g) : 0;
							}
						}
					}
					
					// Check for early termination
					// if cell does not intersect boundary
					if (mask == 0 || mask == 0xff) {
						continue;
					}
					
					// Sum up edge intersections
					int edge_mask = edge_table[mask];
					Vector3 v = new Vector3();
					int e_count = 0;
					
					// For every edge of the cube...
					for (int i = 0; i < 12; ++i) {
						
						// Use edge mask to check if it is crossed
						if (!boolean((edge_mask & (1 << i)))) {
							continue;
						}
						
						// If it did, increment number of edge crossings
						++e_count;
						
						// Now find the point of intersection
						int firstEdgeIndex = i << 1;
						int secondEdgeIndex = firstEdgeIndex + 1;
						// Unpack vertices
						int e0 = cube_edges[firstEdgeIndex];
						int e1 = cube_edges[secondEdgeIndex];
						// Unpack grid values
						float g0 = grid[e0];
						float g1 = grid[e1];
						
						// Compute point of intersection (linear interpolation)
						float t = g0 - g1;
						if (Mathf.Abs(t) > 1e-6) {
							t = g0 / t;
						} else {
							continue;
						}
						
						// Interpolate vertices and add up intersections
						// (this can be done without multiplying)
						for (int j = 0; j < 3; j++) {
							int k = 1 << j; // (1,2,4)
							int a = e0 & k;
							int b = e1 & k;
							if (a != b) {
								v[j] += boolean(a) ? 1.0f - t : t;
							} else {
								v[j] += boolean(a) ? 1.0f : 0f;
							}
						}
					}
					
					// Now we just average the edge intersections
					// and add them to coordinate
					float s = 1.0f / e_count;
					for (int i = 0; i < 3; ++i) {
						v[i] = location[i] + s * v[i];
					}
					
					// Add vertex to buffer, store pointer to
					// vertex index in buffer
					vertexBuffer[m] = vertices.Count;
					vertices.Add(v);
					
					// Now we need to add faces together, to do this we just
					// loop over 3 basis components
					for (int i = 0; i < 3; ++i) {
						
						// The first three entries of the edge_mask
						// count the crossings along the edge
						if (!boolean(edge_mask & (1 << i))) {
							continue;
						}
						
						// i = axes we are point along.
						// iu, iv = orthogonal axes
						int iu = (i + 1) % 3;
						int iv = (i + 2) % 3;
						
						// If we are on a boundary, skip it
						if (location[iu] == 0 || location[iv] == 0) {
							continue;
						}
						
						// Otherwise, look up adjacent edges in buffer
						int du = R[iu];
						int dv = R[iv];
						
						// readonlyly, the indices for the 4 vertices
						// that define the face
						int indexM = vertexBuffer[m];
						int indexMMinusDU = vertexBuffer[m - du];
						int indexMMinusDV = vertexBuffer[m - dv];
						int indexMMinusDUMinusDV = vertexBuffer[m - du - dv];
						
						// Remember to flip orientation depending on the sign
						// of the corner.
						if (boolean(mask & 1)) {
                            triangles.Add(indexM);
                            triangles.Add(indexMMinusDV);
                            triangles.Add(indexMMinusDU);

                            triangles.Add(indexMMinusDV);
                            triangles.Add(indexMMinusDUMinusDV);
                            triangles.Add(indexMMinusDU);
						} else {
                            triangles.Add(indexM);
                            triangles.Add(indexMMinusDU);
                            triangles.Add(indexMMinusDV);

                            triangles.Add(indexMMinusDU);
                            triangles.Add(indexMMinusDUMinusDV);
                            triangles.Add(indexMMinusDV);
						}
					}
				} // end x
			} // end y
		} // end z
		
		//All done!  Return the result
        return new Mesh(vertices.ToArray(), triangles.ToArray());
	}

	/**
     * Converts int to bool.
     *
     * @param i integer to convert
     * @return {@code true} if i > 0; {@code false} otherwise
     */
	private static bool boolean(int i) {
		return i > 0;
	}

} // end SurfaceNets