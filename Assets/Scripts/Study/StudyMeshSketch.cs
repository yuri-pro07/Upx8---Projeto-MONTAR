using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MontAR
{
    /// <summary>
    /// Junta caixas, cilindros e anéis em poucas malhas, uma por material. O PC do estudo tem centenas de
    /// formas (aletas, pás, capacitores): em objetos separados, pesaria no celular.
    /// </summary>
    public sealed class StudyMeshSketch
    {
        private sealed class Layer
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
        }

        private readonly Dictionary<Material, Layer> layers = new Dictionary<Material, Layer>();
        private Bounds bounds;
        private bool hasBounds;

        /// <summary>Quadro em que as próximas formas são descritas (só rotação e translação).</summary>
        public Matrix4x4 Frame { get; set; } = Matrix4x4.identity;

        public Bounds Bounds => bounds;

        /// <summary>Faz um ponto (no quadro atual) contar para os limites sem desenhar nada.</summary>
        public void Include(Vector3 point) => Grow(Frame.MultiplyPoint3x4(point));

        public void Box(Material material, Vector3 center, Vector3 size, float bevel = 0f, Quaternion? rotation = null)
        {
            Layer layer = LayerFor(material);
            Quaternion q = rotation ?? Quaternion.identity;
            Vector3 h = size * 0.5f;
            float b = Mathf.Clamp(bevel, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f);
            Vector3 middle = Point(center);

            Vector3 Corner(Vector3 local) => Point(center + q * local);

            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3;
                int v = (axis + 2) % 3;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 P(int su, int sv)
                    {
                        Vector3 p = default;
                        p[axis] = sign * h[axis];
                        p[u] = su * (h[u] - b);
                        p[v] = sv * (h[v] - b);
                        return Corner(p);
                    }

                    Vector3 a = P(-1, -1);
                    Vector3 c = P(1, 1);
                    Quad(layer, a, P(1, -1), c, P(-1, 1), (a + c) * 0.5f - middle);
                }
            }

            if (b <= 0f)
                return;

            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3;
                int v = (axis + 2) % 3;
                for (int su = -1; su <= 1; su += 2)
                {
                    for (int sv = -1; sv <= 1; sv += 2)
                    {
                        Vector3 E(int sa, float uInset, float vInset)
                        {
                            Vector3 p = default;
                            p[axis] = sa * (h[axis] - b);
                            p[u] = su * (h[u] - uInset);
                            p[v] = sv * (h[v] - vInset);
                            return Corner(p);
                        }

                        Vector3 a = E(-1, 0f, b);
                        Vector3 c = E(1, b, 0f);
                        Quad(layer, a, E(1, 0f, b), c, E(-1, b, 0f), (a + c) * 0.5f - middle);
                    }
                }
            }

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 a = Corner(new Vector3(sx * h.x, sy * (h.y - b), sz * (h.z - b)));
                        Vector3 m = Corner(new Vector3(sx * (h.x - b), sy * h.y, sz * (h.z - b)));
                        Vector3 c = Corner(new Vector3(sx * (h.x - b), sy * (h.y - b), sz * h.z));
                        Triangle(layer, a, m, c, (a + m + c) / 3f - middle);
                    }
                }
            }
        }

        /// <summary>Triângulo achatado, virado para <paramref name="outward"/> (no quadro atual).</summary>
        public void Flat(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Triangle(LayerFor(material), Point(a), Point(b), Point(c), Vec(outward));
        }

        public void Cylinder(Material material, Vector3 center, Vector3 axis, float radius, float length, int segments = 20)
        {
            Layer layer = LayerFor(material);
            Basis(axis, out Vector3 n, out Vector3 u, out Vector3 v);
            Vector3 top = center + n * (length * 0.5f);
            Vector3 bottom = center - n * (length * 0.5f);

            for (int i = 0; i < segments; i++)
            {
                Vector3 d0 = Direction(u, v, i, segments);
                Vector3 d1 = Direction(u, v, i + 1, segments);
                SmoothQuad(layer, top + d0 * radius, top + d1 * radius, bottom + d1 * radius, bottom + d0 * radius, d0, d1);
                Triangle(layer, Point(top), Point(top + d0 * radius), Point(top + d1 * radius), Vec(n));
                Triangle(layer, Point(bottom), Point(bottom + d1 * radius), Point(bottom + d0 * radius), Vec(-n));
            }
        }

        /// <summary>Anel (uma arruela grossa), com furo no meio.</summary>
        public void Ring(Material material, Vector3 center, Vector3 axis, float outer, float inner, float depth, int segments = 36)
        {
            Layer layer = LayerFor(material);
            Basis(axis, out Vector3 n, out Vector3 u, out Vector3 v);
            Vector3 front = n * (depth * 0.5f);

            for (int i = 0; i < segments; i++)
            {
                Vector3 d0 = Direction(u, v, i, segments);
                Vector3 d1 = Direction(u, v, i + 1, segments);
                Vector3 oa = center + d0 * outer, ob = center + d1 * outer, ia = center + d0 * inner, ib = center + d1 * inner;
                Quad(layer, Point(oa + front), Point(ob + front), Point(ib + front), Point(ia + front), Vec(n));
                Quad(layer, Point(oa - front), Point(ob - front), Point(ib - front), Point(ia - front), Vec(-n));
                SmoothQuad(layer, oa + front, ob + front, ob - front, oa - front, d0, d1);
                SmoothQuad(layer, ia + front, ib + front, ib - front, ia - front, -d0, -d1);
            }
        }

        public void Sphere(Material material, Vector3 center, float radius, int rings = 6, int segments = 10)
        {
            Layer layer = LayerFor(material);

            Vector3 Normal(int ring, int segment)
            {
                float theta = Mathf.PI * ring / rings;
                float phi = 2f * Mathf.PI * segment / segments;
                return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    Vector3 n0 = Normal(r, s), n1 = Normal(r, s + 1), n2 = Normal(r + 1, s + 1), n3 = Normal(r + 1, s);
                    int start = layer.Vertices.Count;
                    foreach (Vector3 n in new[] { n0, n1, n2, n3 })
                    {
                        Vector3 p = Point(center + n * radius);
                        Grow(p);
                        layer.Vertices.Add(p);
                        layer.Normals.Add(Vec(n));
                    }
                    Face(layer, start, start + 1, start + 2);
                    Face(layer, start, start + 2, start + 3);
                }
            }
        }

        /// <summary>Tubo que passa pelos pontos, com juntas arredondadas (cabos e heat pipes).</summary>
        public void Tube(Material material, float radius, params Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Sphere(material, points[i], radius);
                if (i == 0)
                    continue;
                Vector3 delta = points[i] - points[i - 1];
                Cylinder(material, (points[i] + points[i - 1]) * 0.5f, delta, radius, delta.magnitude, 10);
            }
        }

        /// <summary>Pás de ventoinha em volta do eixo Z local.</summary>
        public void Blades(Material material, float hubRadius, float tipRadius, float chord, float thickness, int count, float pitch)
        {
            float span = tipRadius - hubRadius;
            for (int i = 0; i < count; i++)
            {
                float angle = 360f * i / count;
                Quaternion turn = Quaternion.AngleAxis(angle, Vector3.forward);
                Box(material, turn * Vector3.right * (hubRadius + span * 0.5f), new Vector3(span, chord, thickness), 0f, turn * Quaternion.AngleAxis(pitch, Vector3.right));
            }
        }

        /// <summary>Cria as malhas como filhos de <paramref name="parent"/>.</summary>
        public void Bake(Transform parent, ICollection<Renderer> renderers)
        {
            foreach (KeyValuePair<Material, Layer> pair in layers)
            {
                var mesh = new Mesh { name = pair.Key.name };
                if (pair.Value.Vertices.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(pair.Value.Vertices);
                mesh.SetNormals(pair.Value.Normals);
                mesh.SetTriangles(pair.Value.Triangles, 0);
                mesh.RecalculateBounds();

                var go = new GameObject(pair.Key.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderers.Add(renderer);
            }
        }

        private Layer LayerFor(Material material)
        {
            if (!layers.TryGetValue(material, out Layer layer))
                layers[material] = layer = new Layer();
            return layer;
        }

        private Vector3 Point(Vector3 local) => Frame.MultiplyPoint3x4(local);

        private Vector3 Vec(Vector3 local) => Frame.MultiplyVector(local).normalized;

        private void Grow(Vector3 point)
        {
            if (hasBounds)
            {
                bounds.Encapsulate(point);
                return;
            }
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
        }

        private static void Basis(Vector3 axis, out Vector3 n, out Vector3 u, out Vector3 v)
        {
            n = axis.normalized;
            u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            v = Vector3.Cross(n, u);
        }

        private static Vector3 Direction(Vector3 u, Vector3 v, int index, int segments)
        {
            float angle = 2f * Mathf.PI * index / segments;
            return u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
        }

        private void Quad(Layer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, d) = (d, b);
                normal = -normal;
            }

            int start = layer.Vertices.Count;
            foreach (Vector3 p in new[] { a, b, c, d })
            {
                Grow(p);
                layer.Vertices.Add(p);
                layer.Normals.Add(normal);
            }
            layer.Triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        private void Triangle(Layer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            int start = layer.Vertices.Count;
            foreach (Vector3 p in new[] { a, b, c })
            {
                Grow(p);
                layer.Vertices.Add(p);
                layer.Normals.Add(normal);
            }
            layer.Triangles.AddRange(new[] { start, start + 1, start + 2 });
        }

        private void SmoothQuad(Layer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normalA, Vector3 normalB)
        {
            int start = layer.Vertices.Count;
            foreach ((Vector3 p, Vector3 n) in new[] { (a, normalA), (b, normalB), (c, normalB), (d, normalA) })
            {
                Vector3 world = Point(p);
                Grow(world);
                layer.Vertices.Add(world);
                layer.Normals.Add(Vec(n));
            }
            Face(layer, start, start + 1, start + 2);
            Face(layer, start, start + 2, start + 3);
        }

        /// <summary>Triângulo com os vértices já na lista, virado para o lado da normal deles.</summary>
        private static void Face(Layer layer, int a, int b, int c)
        {
            Vector3 normal = Vector3.Cross(layer.Vertices[b] - layer.Vertices[a], layer.Vertices[c] - layer.Vertices[a]);
            if (Vector3.Dot(normal, layer.Normals[a] + layer.Normals[b] + layer.Normals[c]) < 0f)
                (b, c) = (c, b);
            layer.Triangles.Add(a);
            layer.Triangles.Add(b);
            layer.Triangles.Add(c);
        }
    }
}
