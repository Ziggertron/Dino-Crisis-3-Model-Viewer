using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DC3 {
public static class Data {
    public static Dictionary<string,object> Obj(object o) { return (Dictionary<string,object>)o; }
    public static object[] Arr(object o) { return (object[])o; }
    public static int Int(object o) { return Convert.ToInt32(o); }
    public static double Num(object o) { return Convert.ToDouble(o); }
    public static int GetInt(Dictionary<string,object> o,string k,int d=0) { return o.ContainsKey(k)?Int(o[k]):d; }
    public static string Name(Dictionary<string,object> o,string d) { return o.ContainsKey("name")?(string)o["name"]:d; }
}
public class Node {
    public string Name; public int Parent=-1; public int[] Children=new int[0]; public bool Bone;
    public Vector3D T, S=new Vector3D(1,1,1), BaseT,BaseS; public Quaternion Q=Quaternion.Identity,BaseQ;
    public Matrix3D World;
    public void Reset() { T=BaseT; S=BaseS; Q=BaseQ; }
}
public class Track { public int Node; public string Path; public float[][] Times,Values; public bool Step; }
public class Clip { public string Name; public double Duration; public List<Track> Tracks=new List<Track>(); public override string ToString(){return Name+"  /  "+Duration.ToString("0.00")+"s";} }
public class Part {
    public string Name; public int Owner; public int[] Joints; public Matrix3D[] Inverse;
    public float[][] Positions, Normals, Weights,JointIndices; public int[] Indices;
    public MeshGeometry3D Geometry; public GeometryModel3D Visual; public Material OriginalMaterial;
    public Point3D[] Current; public bool Visible=true;
}
public class Model {
    public Node[] Nodes; public List<Part> Parts=new List<Part>(); public List<Clip> Clips=new List<Clip>();
    public List<BitmapSource> Textures=new List<BitmapSource>(); public List<string> TextureNames=new List<string>();
    public Model3DGroup Visual=new Model3DGroup(); public string Source; public int VertexCount,TriangleCount;
    Dictionary<string,object> Doc; byte[] Bin; Dictionary<int,float[][]> Cache=new Dictionary<int,float[][]>();
    public static Matrix3D Matrix(float[] a) { return new Matrix3D(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8],a[9],a[10],a[11],a[12],a[13],a[14],a[15]); }
    static Vector3D Vec(object o) { var a=Data.Arr(o); return new Vector3D(Data.Num(a[0]),Data.Num(a[1]),Data.Num(a[2])); }
    public static Material Neutral() { return new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(174,190,193))); }
    public static Material Textured(BitmapSource image) { var brush=new ImageBrush(image); brush.ViewportUnits=BrushMappingMode.Absolute; brush.TileMode=TileMode.Tile; return new DiffuseMaterial(brush); }
    public static Model Load(string path) {
        var model=new Model();model.Source=path;
        byte[] bytes=File.ReadAllBytes(path);
        if(bytes.Length<20||BitConverter.ToUInt32(bytes,0)!=0x46546c67||BitConverter.ToUInt32(bytes,4)!=2||BitConverter.ToUInt32(bytes,8)!=bytes.Length)throw new InvalidDataException("Expected a complete GLB 2.0 file.");
        string json=null;
        for(int at=12;at<bytes.Length;) { if(at+8>bytes.Length)throw new InvalidDataException("Truncated GLB chunk.");int size=checked((int)BitConverter.ToUInt32(bytes,at));uint kind=BitConverter.ToUInt32(bytes,at+4);at+=8;if(size<0||size>bytes.Length-at)throw new InvalidDataException("Invalid GLB chunk size.");if(kind==0x4e4f534a)json=Encoding.UTF8.GetString(bytes,at,size);if(kind==0x004e4942){model.Bin=new byte[size];Buffer.BlockCopy(bytes,at,model.Bin,0,size);}at+=size; }
        if(json==null||model.Bin==null)throw new InvalidDataException("This viewer requires a self-contained GLB.");
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=int.MaxValue;serializer.RecursionLimit=256;model.Doc=Data.Obj(serializer.DeserializeObject(json));model.Read();model.Cache.Clear();model.Doc=null;model.Bin=null;return model;
    }
    float[][] Access(int index) {
        if(Cache.ContainsKey(index))return Cache[index];
        var a=Data.Obj(Data.Arr(Doc["accessors"])[index]);if(a.ContainsKey("sparse"))throw new InvalidDataException("Sparse accessors are not supported.");
        string type=(string)a["type"];int width=type=="SCALAR"?1:type=="VEC2"?2:type=="VEC3"?3:type=="VEC4"?4:type=="MAT4"?16:0;
        if(width==0)throw new InvalidDataException("Unsupported accessor type.");int component=Data.Int(a["componentType"]);int unit=component==5126||component==5125?4:component==5123||component==5122?2:1;
        if(!new[]{5120,5121,5122,5123,5125,5126}.Contains(component))throw new InvalidDataException("Unsupported component type.");
        var view=Data.Obj(Data.Arr(Doc["bufferViews"])[Data.Int(a["bufferView"])]);if(Data.GetInt(view,"buffer")!=0)throw new InvalidDataException("External buffers are not supported.");
        int start=checked(Data.GetInt(view,"byteOffset")+Data.GetInt(a,"byteOffset")), count=Data.Int(a["count"]),stride=Data.GetInt(view,"byteStride",width*unit);
        if(count<0||count>20000000||stride<width*unit||start<0||(long)start+(long)Math.Max(0,count-1)*stride+width*unit>Bin.Length)throw new InvalidDataException("Accessor exceeds the GLB buffer.");
        bool normalized=a.ContainsKey("normalized")&&(bool)a["normalized"];var values=new float[count][];
        for(int i=0;i<count;i++){values[i]=new float[width];for(int c=0;c<width;c++){int p=start+i*stride+c*unit;float v=component==5126?BitConverter.ToSingle(Bin,p):component==5125?BitConverter.ToUInt32(Bin,p):component==5123?BitConverter.ToUInt16(Bin,p):component==5122?BitConverter.ToInt16(Bin,p):component==5120?(float)(sbyte)Bin[p]:(float)Bin[p];if(normalized){if(component==5121)v/=255f;if(component==5123)v/=65535f;if(component==5120)v=Math.Max(-1,v/127f);if(component==5122)v=Math.Max(-1,v/32767f);}if(float.IsNaN(v)||float.IsInfinity(v))throw new InvalidDataException("Non-finite model data.");values[i][c]=v;}}
        Cache[index]=values;return values;
    }
    void Read() {
        var rawNodes=Data.Arr(Doc["nodes"]);Nodes=new Node[rawNodes.Length];
        for(int i=0;i<Nodes.Length;i++){var n=Data.Obj(rawNodes[i]);var node=new Node();Nodes[i]=node;node.Name=Data.Name(n,"Node "+i);if(n.ContainsKey("matrix"))throw new InvalidDataException("Matrix-based nodes are unsupported. Use the included converter's TRS GLBs.");if(n.ContainsKey("translation"))node.T=Vec(n["translation"]);if(n.ContainsKey("scale"))node.S=Vec(n["scale"]);if(n.ContainsKey("rotation")){var q=Data.Arr(n["rotation"]);node.Q=new Quaternion(Data.Num(q[0]),Data.Num(q[1]),Data.Num(q[2]),Data.Num(q[3]));}node.BaseT=node.T;node.BaseS=node.S;node.BaseQ=node.Q;if(n.ContainsKey("children"))node.Children=Data.Arr(n["children"]).Select(Data.Int).ToArray();}
        for(int i=0;i<Nodes.Length;i++)foreach(int child in Nodes[i].Children){if(child<0||child>=Nodes.Length||Nodes[child].Parent!=-1)throw new InvalidDataException("Invalid node hierarchy.");Nodes[child].Parent=i;}
        if(Doc.ContainsKey("textures"))foreach(object item in Data.Arr(Doc["textures"])){
            var tex=Data.Obj(item);var im=Data.Obj(Data.Arr(Doc["images"])[Data.Int(tex["source"])]);if(!im.ContainsKey("bufferView"))throw new InvalidDataException("External textures are not supported.");var view=Data.Obj(Data.Arr(Doc["bufferViews"])[Data.Int(im["bufferView"])]);int offset=Data.GetInt(view,"byteOffset"),length=Data.Int(view["byteLength"]);using(var stream=new MemoryStream(Bin,offset,length)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();Textures.Add(bitmap);TextureNames.Add(Data.Name(im,"Texture "+(Textures.Count-1)));}}
        for(int ni=0;ni<Nodes.Length;ni++){
            var n=Data.Obj(rawNodes[ni]);if(!n.ContainsKey("mesh"))continue;var mesh=Data.Obj(Data.Arr(Doc["meshes"])[Data.Int(n["mesh"])]);
            foreach(object primitive in Data.Arr(mesh["primitives"])){
                var p=Data.Obj(primitive);if(Data.GetInt(p,"mode",4)!=4)throw new InvalidDataException("Only triangle meshes are supported.");var attr=Data.Obj(p["attributes"]);var part=new Part();part.Name=Data.Name(mesh,"Mesh "+Parts.Count);part.Owner=ni;part.Positions=Access(Data.Int(attr["POSITION"]));part.Normals=attr.ContainsKey("NORMAL")?Access(Data.Int(attr["NORMAL"])):null;part.Indices=p.ContainsKey("indices")?Access(Data.Int(p["indices"])).Select(x=>(int)x[0]).ToArray():Enumerable.Range(0,part.Positions.Length).ToArray();
                if(part.Indices.Length%3!=0||part.Indices.Any(x=>x<0||x>=part.Positions.Length))throw new InvalidDataException("Invalid triangle indices.");
                part.Geometry=new MeshGeometry3D();part.Geometry.TriangleIndices=new Int32Collection(part.Indices);if(attr.ContainsKey("TEXCOORD_0"))part.Geometry.TextureCoordinates=new PointCollection(Access(Data.Int(attr["TEXCOORD_0"])).Select(x=>new Point(x[0],x[1])));
                part.OriginalMaterial=Neutral();
                if(p.ContainsKey("material")){var mat=Data.Obj(Data.Arr(Doc["materials"])[Data.Int(p["material"])]);if(mat.ContainsKey("pbrMetallicRoughness")){var pbr=Data.Obj(mat["pbrMetallicRoughness"]);if(pbr.ContainsKey("baseColorTexture")){int idx=Data.Int(Data.Obj(pbr["baseColorTexture"])["index"]);part.OriginalMaterial=Textured(Textures[idx]);}}}
                if(n.ContainsKey("skin")){
                    var skin=Data.Obj(Data.Arr(Doc["skins"])[Data.Int(n["skin"])]);part.Joints=Data.Arr(skin["joints"]).Select(Data.Int).ToArray();part.Inverse=skin.ContainsKey("inverseBindMatrices")?Access(Data.Int(skin["inverseBindMatrices"])).Select(Matrix).ToArray():Enumerable.Repeat(Matrix3D.Identity,part.Joints.Length).ToArray();
                    part.JointIndices=Access(Data.Int(attr["JOINTS_0"]));part.Weights=Access(Data.Int(attr["WEIGHTS_0"]));if(part.Inverse.Length!=part.Joints.Length||part.JointIndices.Length!=part.Positions.Length||part.Weights.Length!=part.Positions.Length)throw new InvalidDataException("Invalid skin sizes.");foreach(int joint in part.Joints){if(joint<0||joint>=Nodes.Length)throw new InvalidDataException("Invalid joint.");Nodes[joint].Bone=true;}
                }
                part.Visual=new GeometryModel3D(part.Geometry,part.OriginalMaterial);part.Visual.BackMaterial=part.OriginalMaterial;Visual.Children.Add(part.Visual);Parts.Add(part);VertexCount+=part.Positions.Length;TriangleCount+=part.Indices.Length/3;
            }
        }
        if(Doc.ContainsKey("animations"))foreach(object item in Data.Arr(Doc["animations"])){
            var a=Data.Obj(item);var clip=new Clip();clip.Name=Data.Name(a,"Clip "+Clips.Count);var samplers=Data.Arr(a["samplers"]);
            foreach(object channel in Data.Arr(a["channels"])){
                var ch=Data.Obj(channel);var target=Data.Obj(ch["target"]);string path=(string)target["path"];if(path!="translation"&&path!="rotation"&&path!="scale")continue;var s=Data.Obj(samplers[Data.Int(ch["sampler"])]);string interpolation=s.ContainsKey("interpolation")?(string)s["interpolation"]:"LINEAR";if(interpolation!="LINEAR"&&interpolation!="STEP")throw new InvalidDataException("Use linear or step animation clips.");
                var track=new Track{Node=Data.Int(target["node"]),Path=path,Times=Access(Data.Int(s["input"])),Values=Access(Data.Int(s["output"])),Step=interpolation=="STEP"};if(track.Node<0||track.Node>=Nodes.Length||track.Times.Length==0||track.Times.Length!=track.Values.Length)throw new InvalidDataException("Invalid animation channel.");clip.Duration=Math.Max(clip.Duration,track.Times.Last()[0]);clip.Tracks.Add(track);
            }Clips.Add(clip);
        }
        Pose(-1,0);
    }
    void World(int i,int[] state) {
        if(state[i]==2)return;if(state[i]==1)throw new InvalidDataException("Cyclic node hierarchy.");state[i]=1;var n=Nodes[i];var m=Matrix3D.Identity;m.Scale(n.S);m.Rotate(n.Q);m.Translate(n.T);if(n.Parent>=0){World(n.Parent,state);m.Append(Nodes[n.Parent].World);}n.World=m;state[i]=2;
    }
    public void Pose(int clipIndex,double time) {
        foreach(var n in Nodes)n.Reset();
        if(clipIndex>=0){foreach(var t in Clips[clipIndex].Tracks){int lo=0,hi=t.Times.Length-1;while(lo<hi){int mid=(lo+hi+1)/2;if(t.Times[mid][0]<=time)lo=mid;else hi=mid-1;}int b=Math.Min(lo+1,t.Times.Length-1);double f=t.Step||lo==b?0:Math.Max(0,Math.Min(1,(time-t.Times[lo][0])/(t.Times[b][0]-t.Times[lo][0])));var a=t.Values[lo];var v=t.Values[b];var n=Nodes[t.Node];if(t.Path=="rotation")n.Q=Quaternion.Slerp(new Quaternion(a[0],a[1],a[2],a[3]),new Quaternion(v[0],v[1],v[2],v[3]),f);else{var vec=new Vector3D(a[0]+(v[0]-a[0])*f,a[1]+(v[1]-a[1])*f,a[2]+(v[2]-a[2])*f);if(t.Path=="translation")n.T=vec;else n.S=vec;}}}
        var state=new int[Nodes.Length];for(int i=0;i<Nodes.Length;i++)World(i,state);
        foreach(var part in Parts){var positions=new Point3DCollection(part.Positions.Length);var normals=new Vector3DCollection(part.Positions.Length);Matrix3D[] matrices=null;if(part.Joints!=null){matrices=new Matrix3D[part.Joints.Length];for(int j=0;j<matrices.Length;j++){matrices[j]=part.Inverse[j];matrices[j].Append(Nodes[part.Joints[j]].World);}}
            for(int i=0;i<part.Positions.Length;i++){var p=part.Positions[i];var original=new Point3D(p[0],p[1],p[2]);var originalNormal=part.Normals==null?new Vector3D():new Vector3D(part.Normals[i][0],part.Normals[i][1],part.Normals[i][2]);Point3D result;Vector3D normal;
                if(matrices!=null){result=new Point3D();normal=new Vector3D();for(int j=0;j<part.Weights[i].Length;j++){double weight=part.Weights[i][j];if(weight==0)continue;int joint=(int)part.JointIndices[i][j];if(joint<0||joint>=matrices.Length)throw new InvalidDataException("Invalid mesh-local joint index.");var tp=matrices[joint].Transform(original);result+=new Vector3D(tp.X,tp.Y,tp.Z)*weight;normal+=TransformNormal(matrices[joint],originalNormal)*weight;}}
                else {result=Nodes[part.Owner].World.Transform(original);normal=TransformNormal(Nodes[part.Owner].World,originalNormal);}positions.Add(result);if(normal.LengthSquared>1e-20)normal.Normalize();normals.Add(normal);
            }part.Current=positions.ToArray();part.Geometry.Positions=positions;if(part.Normals!=null)part.Geometry.Normals=normals;
        }
    }
    static Vector3D TransformNormal(Matrix3D matrix,Vector3D normal){if(!matrix.HasInverse)return matrix.Transform(normal);matrix.Invert();return new Vector3D(matrix.M11*normal.X+matrix.M12*normal.Y+matrix.M13*normal.Z,matrix.M21*normal.X+matrix.M22*normal.Y+matrix.M23*normal.Z,matrix.M31*normal.X+matrix.M32*normal.Y+matrix.M33*normal.Z);}
    public Rect3D Bounds(){var bounds=Rect3D.Empty;foreach(var part in Parts)foreach(var p in part.Current)bounds.Union(p);return bounds;}
}
}

