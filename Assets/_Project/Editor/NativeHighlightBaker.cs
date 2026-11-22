using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Source HighlightRender generates this texture at runtime. This Editor-side
// compatibility bake retains source geometry, projection/size/blur constants.
// The original compiled blur/composite kernels are unavailable; those kernels are
// disclosed reconstructions. No artist-drawn outline or animation is generated.
public static class NativeHighlightBaker
{
    struct Triangle { public Vector2 a,b,c; }
    public static Texture2D Bake(Transform sourceRoot,MeshRenderer plane,IEnumerable<MeshRenderer> include,string path)
    {
        if(EditorApplication.isPlaying)throw new Exception("Highlight bake requires Edit mode");
        var pm=plane.GetComponent<MeshFilter>().sharedMesh;var vertices=pm.vertices;
        var uv=pm.uv2.Length==pm.vertexCount?pm.uv2:pm.uv;
        var points=vertices.Select(v=>sourceRoot.InverseTransformPoint(plane.transform.TransformPoint(v))).ToArray();
        int a=0,b=-1,c=-1;
        for(int i=1;i<points.Length;i++)if((uv[i]-uv[0]).sqrMagnitude>1e-8f){b=i;break;}
        for(int i=1;i<points.Length;i++)if(b>=0&&Mathf.Abs(Cross(uv[b]-uv[0],uv[i]-uv[0]))>1e-6f){c=i;break;}
        if(c<0)throw new Exception("Original highlight plane has no usable UV projection");
        var pa=new Vector2(points[a].x,points[a].z);var pb=new Vector2(points[b].x,points[b].z);var pc=new Vector2(points[c].x,points[c].z);
        float determinant=Cross(pb-pa,pc-pa);if(Mathf.Abs(determinant)<1e-8f)throw new Exception("Nonplanar highlight projection");
        Vector2 Map(Vector3 p)
        {
            var d=new Vector2(p.x,p.z)-pa;float s=Cross(d,pc-pa)/determinant,t=Cross(pb-pa,d)/determinant;
            return uv[a]+s*(uv[b]-uv[a])+t*(uv[c]-uv[a]);
        }
        var triangles=new List<Triangle>();
        foreach(var r in include)
        {
            if(r==plane)continue;var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
            var ps=mesh.vertices.Select(v=>Map(sourceRoot.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)triangles.Add(new Triangle{a=ps[indices[i]],b=ps[indices[i+1]],c=ps[indices[i+2]]});
        }
        var bounds=plane.bounds;float w=bounds.size.x,h=Mathf.Max(bounds.size.y,bounds.size.z);float ortho=h*.5f;
        int width=w>h?256:Mathf.Max(1,(int)(256*w/h)),height=w>h?Mathf.Max(1,(int)(256*h/w)):256;
        var x=Render(triangles,(int)(width*.3f),(int)(height*.3f),ortho, .2f);
        var y=Render(triangles,(int)(width*.3f),(int)(height*.3f),ortho, .25f);
        var first=Blur(Max(x,y),1.25f);
        var second=Render(triangles,(int)(width*.5f),(int)(height*.5f),ortho,.01f);
        var mid=Blur(Max(Resize(first,second.GetLength(0),second.GetLength(1)),second),1.25f);
        var third=Render(triangles,width,height,ortho,-.05f);
        var full=Blur(Max(Resize(mid,width,height),third),1);
        full=Blur(full,1.5f);
        var clip=Resize(Render(triangles,(int)(width*.92f),(int)(height*.92f),ortho,-.1f),width,height);
        var pixels=new Color[width*height];
        for(int j=0;j<height;j++)for(int i=0;i<width;i++)
        {
            float v=Mathf.Clamp01(full[i,j]*(1-clip[i,j]));pixels[j*width+i]=new Color(v,v,v,1);
        }
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false,true);texture.SetPixels(pixels);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float[,] Render(List<Triangle> triangles,int width,int height,float ortho,float delta)
    {
        var buffer=new float[Mathf.Max(1,width),Mathf.Max(1,height)];float factor=ortho/(ortho-delta);
        foreach(var triangle in triangles)
        {
            Vector2 a=(triangle.a-Vector2.one*.5f)*factor+Vector2.one*.5f,b=(triangle.b-Vector2.one*.5f)*factor+Vector2.one*.5f,c=(triangle.c-Vector2.one*.5f)*factor+Vector2.one*.5f;
            a=Vector2.Scale(a,new Vector2(width,height));b=Vector2.Scale(b,new Vector2(width,height));c=Vector2.Scale(c,new Vector2(width,height));float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-8f)continue;
            int left=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,b.x,c.x)),0,width-1),right=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,b.x,c.x)),0,width-1),bottom=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,b.y,c.y)),0,height-1),top=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,b.y,c.y)),0,height-1);
            for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++){var p=new Vector2(x+.5f,y+.5f);float u=Cross(b-p,c-p)/area,v=Cross(c-p,a-p)/area;if(u>=0&&v>=0&&u+v<=1)buffer[x,y]=1;}
        }
        return buffer;
    }
    static float[,] Max(float[,] a,float[,] b)
    {
        int w=a.GetLength(0),h=a.GetLength(1);var r=new float[w,h];for(int j=0;j<h;j++)for(int i=0;i<w;i++)r[i,j]=Mathf.Max(a[i,j],b[i,j]);return r;
    }
    static float[,] Resize(float[,] image,int width,int height)
    {
        var r=new float[width,height];for(int j=0;j<height;j++)for(int i=0;i<width;i++)r[i,j]=Read(image,(i+.5f)*image.GetLength(0)/width-.5f,(j+.5f)*image.GetLength(1)/height-.5f);return r;
    }
    static float Read(float[,] image,float x,float y)
    {
        int w=image.GetLength(0),h=image.GetLength(1),i=Mathf.FloorToInt(x),j=Mathf.FloorToInt(y);float fx=x-i,fy=y-j;
        float At(int a,int b)=>image[Mathf.Clamp(a,0,w-1),Mathf.Clamp(b,0,h-1)];return Mathf.Lerp(Mathf.Lerp(At(i,j),At(i+1,j),fx),Mathf.Lerp(At(i,j+1),At(i+1,j+1),fx),fy);
    }
    static float[,] Blur(float[,] image,float offset)
    {
        int w=image.GetLength(0),h=image.GetLength(1);var r=new float[w,h];for(int j=0;j<h;j++)for(int i=0;i<w;i++)r[i,j]=(Read(image,i-offset,j-offset)+Read(image,i+offset,j-offset)+Read(image,i-offset,j+offset)+Read(image,i+offset,j+offset))*.25f;return r;
    }
}
