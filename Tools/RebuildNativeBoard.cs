// Rejected first study, retained for diagnosis. Never run this against the playable scene.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RebuildNativeBoard
{
    const string Art = "Assets/_Project/Art/NativeBoard";
    const int BoardLayer = 30;
    static Transform stage;
    static Material ground, props, table, trays, hero, playing;
    static readonly Color White = Color.white;
    static readonly float[] X = {-700,-650,-550,-400,-300,-205,-130,0,130,205,300,400,550,650,700};
    static readonly float[] Top = {240,270,300,345,370,335,287,267,287,335,370,345,300,270,240};
    static readonly float[] Bottom = {-210,-240,-270,-310,-335,-297,-229,-205,-229,-297,-335,-310,-270,-240,-210};

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/NativeBoardStudy.unity") throw new InvalidOperationException("Experimental authoring only: open NativeBoardStudy. The playable HearthstoneTable must not be replaced by this study.");
        Directory.CreateDirectory(Art + "/Meshes");
        foreach (string file in new[]{"ORG_GroundCorners.png","ORG_Combined.png","STW_Table.png","Board_HeroTray.png","HeroFrame_Silver.png","VarianLL_Playspace.png"})
        {
            string destination = Art + "/" + file;
            if (!File.Exists(destination)) File.Copy("/home/craig-middleton/Desktop/Hearthstone Export/Texture2D/" + file, destination);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
            importer.textureType = TextureImporterType.Default; importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        }
        ground = Paint("Ground", "ORG_GroundCorners.png"); props = Paint("Scenery", "ORG_Combined.png");
        table = Paint("Table", "STW_Table.png"); trays = Paint("HeroTrays", "Board_HeroTray.png"); hero = Paint("HeroStone", "HeroFrame_Silver.png");
        playing = Paint("PlayingSurface", "VarianLL_Playspace.png");
        props.SetFloat("_ClipBackdrop",1);props.SetFloat("_ReliefLight",.6f);
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var surface = GameObject.Find("GameCanvas").transform.Find("TableSurface");
        var themes = surface.Find("BoardThemes").GetComponent<RandomBoardTheme>();
        var prior = themes.transform.Find("Orgrimmar Rebuilt");
        if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
        var root = new GameObject("Orgrimmar Rebuilt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(NativeBoardView));
        root.SetActive(false); root.transform.SetParent(themes.transform, false);
        ((RectTransform)root.transform).sizeDelta = new Vector2(1920,1080);
        var view = root.GetComponent<NativeBoardView>(); view.display = root.GetComponent<RawImage>(); view.display.raycastTarget = false;
        stage = new GameObject("Native board geometry").transform; stage.SetParent(root.transform, false); view.stage = stage;
        var camera = new GameObject("Board capture camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.SetParent(stage, false); camera.transform.localPosition = new Vector3(0,0,-1800);
        camera.orthographic = true; camera.orthographicSize = 540; camera.nearClipPlane = 1; camera.farClipPlane = 3000;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f,.04f,.025f);
        camera.cullingMask = 1 << BoardLayer; camera.enabled = false; camera.allowHDR = false; camera.allowMSAA = true; view.boardCamera = camera;
        if (Camera.main != null) Camera.main.cullingMask &= ~(1 << BoardLayer);

        Quad("Wood and blue cloth", table, new Rect(-960,-540,1920,1080), new Rect(0,0,2048,1880), 150);
        // Independent corner foundations; each samples only its own ground island.
        Quad("Upper left cracked-earth plinth", ground, new Rect(-735,255,610,315), new Rect(78,82,420,270), 46);
        Quad("Upper right cracked-earth plinth", ground, new Rect(125,255,610,315), new Rect(538,82,405,270), 46);
        Quad("Lower left cracked-earth plinth", ground, new Rect(-735,-535,610,310), new Rect(78,624,420,270), 46);
        Quad("Lower right cracked-earth plinth", ground, new Rect(125,-535,610,310), new Rect(538,624,405,270), 46);
        Field();
        var boundary = X.Select((x,i)=>new Vector2(x,Top[i])).Concat(X.Select((x,i)=>new Vector2(x,Bottom[i])).Reverse()).ToArray();
        Border(boundary);
        Quad("Left outside stone rail", ground, new Rect(-770,-500,60,1000), new Rect(10,70,56,825), 25);
        Quad("Right outside stone rail", ground, new Rect(710,-500,75,1000), new Rect(10,70,56,825), 25);
        HeroTrays(); HeroSocket(game.faceView); HeroSocket(game.opponentFaceView);
        DeckWell("Upper deck recess", 755, 260); DeckWell("Lower deck recess", 755,-165);
        Scenery();
        // The previous generated plates remain as the other two themes during this first reconstruction checkpoint.
        var oldClassic = themes.boards[0]; themes.boards[0] = root;
        if (oldClassic != null && oldClassic != root) oldClassic.SetActive(false);
        themes.ShowBoard(0);
        AlignPocket(surface, "OpponentDeck", 755,260); AlignPocket(surface,"PlayerDeck",755,-165);
        view.replacedDecoration = new[]{"OpponentDeck","PlayerDeck"}.SelectMany(n=>new[]{"StoneRim","Recess"}.Select(c=>surface.Find("DeckPockets/"+n+"/"+c).gameObject)).ToArray();
        // Move only decorative authoring controls into the new selected hierarchy for inspection.
        Selection.activeGameObject = root;
        foreach (var c in game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorUtility.SetDirty(themes); EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        return "Rebuilt classic board from native textures: independent terrain, raised stone boundary, hero trays/sockets, two deck recesses and separate relief-mesh scenery. Existing UI and other two themes retained.";
    }

    static Material Paint(string name, string file)
    {
        string path = Art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("HearthClone/Board/Native Surface")); AssetDatabase.CreateAsset(material,path); }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/" + file); EditorUtility.SetDirty(material); return material;
    }
    static Vector2 UV(Material material, Vector2 pixel) => new Vector2(pixel.x/material.mainTexture.width, 1-pixel.y/material.mainTexture.height);
    static void MeshObject(string name, Material material, List<Vector3> vertices, List<Vector2> uv, List<int> triangles, List<Color> colors)
    {
        var mesh = new Mesh {name=name}; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.SetColors(colors); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + "/Meshes/" + name.Replace(" ","_") + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old == null) AssetDatabase.CreateAsset(mesh,path); else {EditorUtility.CopySerialized(mesh,old); UnityEngine.Object.DestroyImmediate(mesh); mesh=old; EditorUtility.SetDirty(mesh);}
        var go = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.layer=BoardLayer; go.transform.SetParent(stage,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Quad(string name, Material material, Rect destination, Rect source, float depth)
    {
        var p = new[]{new Vector2(destination.xMin,destination.yMin),new Vector2(destination.xMax,destination.yMin),new Vector2(destination.xMax,destination.yMax),new Vector2(destination.xMin,destination.yMax)};
        var uv = new[]{UV(material,new Vector2(source.xMin,source.yMax)),UV(material,new Vector2(source.xMax,source.yMax)),UV(material,new Vector2(source.xMax,source.yMin)),UV(material,new Vector2(source.xMin,source.yMin))};
        MeshObject(name,material,p.Select(v=>new Vector3(v.x,v.y,depth)).ToList(),uv.ToList(),new List<int>{0,1,2,0,2,3},Enumerable.Repeat(White,4).ToList());
    }
    static void Field()
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>(); var colors=new List<Color>();
        const int rows=16;
        for(int i=0;i<X.Length;i++)for(int j=0;j<=rows;j++)
        {
            float u=(X[i]+700)/1400, v=j/(float)rows;
            float[] sourceTop={125,115,84,45,20,55,85,85,85,55,20,45,84,115,125};
            float[] sourceBottom={366,380,420,465,480,460,416,395,416,460,480,465,420,380,366};
            vertices.Add(new Vector3(X[i],v<.5f?Mathf.Lerp(Bottom[i],20,v*2):Mathf.Lerp(20,Top[i],v*2-1),0));
            float sx=Mathf.Lerp(35,989,u)-Mathf.Clamp01((u-.9f)*10)*75*(1-Mathf.Abs(v*2-1));
            float sy=v<.5f?Mathf.Lerp(sourceBottom[i]-14,232,v*2):Mathf.Lerp(232,sourceTop[i]+14,v*2-1);
            uv.Add(UV(playing,new Vector2(sx,sy))); colors.Add(White);
            if(i==0||j==0)continue; int b=i*(rows+1)+j;
            triangles.AddRange(new[]{b,b-1,b-rows-2,b,b-rows-2,b-rows-1});
        }
        MeshObject("Shaped central playing surface",playing,vertices,uv,triangles,colors);
    }
    static Vector2[] Expand(Vector2[] polygon,float distance)
    {
        var result=new Vector2[polygon.Length];
        // The boundary is clockwise; the left side of each edge faces outwards.
        for(int i=0;i<polygon.Length;i++)
        {
            Vector2 a=(polygon[i]-polygon[(i+polygon.Length-1)%polygon.Length]).normalized;
            Vector2 b=(polygon[(i+1)%polygon.Length]-polygon[i]).normalized;
            Vector2 n1=new Vector2(-a.y,a.x),n2=new Vector2(-b.y,b.x),n=(n1+n2).normalized;
            result[i]=polygon[i]+n*distance/Mathf.Max(.45f,Vector2.Dot(n,n2));
        }
        return result;
    }
    static void Border(Vector2[] boundary)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var colors=new List<Color>();
        float[] widths={0,5,13,26,37}; float[] depth={-1,-9,-15,-10,16};float[] tint={.46f,.94f,1.12f,.84f,.40f};
        for(int band=0;band<widths.Length;band++)
        {
            var contour=Expand(boundary,widths[band]);
            for(int i=0;i<boundary.Length;i++)
            {
                vertices.Add(new Vector3(contour[i].x,contour[i].y,depth[band]));
                uv.Add(UV(ground,new Vector2(23+band*8,100+(i/(float)boundary.Length)*740)));
                colors.Add(new Color(tint[band],tint[band]*.94f,tint[band]*.83f));
                if(band==0)continue;int k=band*boundary.Length+i,next=band*boundary.Length+(i+1)%boundary.Length;
                triangles.AddRange(new[]{k,next,next-boundary.Length,k,next-boundary.Length,k-boundary.Length});
            }
        }
        MeshObject("Raised bevelled playing-space border",ground,vertices,uv,triangles,colors);
    }
    static Vector2[] Points(params float[] xy) { var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p; }

    // Ear clipping retains the concavities between horns, wings, and hero openings.
    static List<int> Triangulate(Vector2[] p)
    {
        var result=new List<int>(); var remaining=Enumerable.Range(0,p.Length).ToList();
        float area=0;for(int i=0;i<p.Length;i++)area+=Cross(p[i],p[(i+1)%p.Length]);
        float sign=Mathf.Sign(area);int guard=p.Length*p.Length;
        while(remaining.Count>3&&guard-->0)
        {
            bool found=false;
            for(int i=0;i<remaining.Count;i++)
            {
                int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                if(Cross(p[b]-p[a],p[c]-p[b])*sign<=.001f)continue;
                bool inside=remaining.Any(k=>k!=a&&k!=b&&k!=c&&Inside(p[k],p[a],p[b],p[c],sign));
                if(inside)continue;result.AddRange(new[]{a,b,c});remaining.RemoveAt(i);found=true;break;
            }
            if(!found)throw new InvalidOperationException("Scenery contour cannot be triangulated");
        }
        if(remaining.Count==3)result.AddRange(remaining);return result;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c,float sign)=>Cross(b-a,p-a)*sign>=0&&Cross(c-b,p-b)*sign>=0&&Cross(a-c,p-c)*sign>=0;
    static void Cutout(string name,Material material,Vector2[] pixels,Func<Vector2,Vector2> place,float depth,float relief=0)
    {
        var points=pixels.Select(place).ToArray();var v=new List<Vector3>();var uv=new List<Vector2>();var color=new List<Color>();var triangles=Triangulate(points);
        for(int i=0;i<points.Length;i++){v.Add(new Vector3(points[i].x,points[i].y,depth));uv.Add(UV(material,pixels[i]));color.Add(White);}
        // Raised interior vertices give each prop actual depth, preserving the native painted silhouette.
        if(relief>0)
        {
            var subdivided=new List<int>();
            for(int i=0;i<triangles.Count;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2],k=v.Count;
                Vector3 center=(v[a]+v[b]+v[c])/3;center.z-=relief;v.Add(center);uv.Add((uv[a]+uv[b]+uv[c])/3);color.Add(White);
                subdivided.AddRange(new[]{a,b,k,b,c,k,c,a,k});
            }
            triangles=subdivided;
        }
        MeshObject(name,material,v,uv,triangles,color);
    }
    static void HeroTrays()
    {
        var lower=Points(4,373,52,370,152,321,227,276,256,262,285,276,360,321,460,370,508,373,488,401,479,441,499,489,459,480,357,437,317,417,316,355,301,324,279,305,256,294,233,306,211,328,196,356,196,415,150,435,50,479,5,494,26,445,28,410);
        Cutout("Purple lower hero tray",trays,lower,p=>new Vector2((p.x-256)*1.68f,-204-(p.y-294)*1.68f),-25);
        var upper=Points(4,24,49,38,147,74,178,105,181,191,317,191,320,105,350,74,463,34,507,24,480,67,478,116,507,161,469,155,371,194,330,215,174,215,135,194,44,155,5,161,33,116,33,68);
        Cutout("Red upper hero tray",trays,upper,p=>new Vector2((p.x-249)*1.68f,282-(p.y-191)*1.68f),-25);
    }
    static void HeroSocket(FaceView face)
    {
        var mask=(RectTransform)face.avatarImage.transform.parent;
        var shape=mask.GetComponent<PortraitSilhouette>();var p=shape.outline;
        var position=((RectTransform)face.transform).anchoredPosition;var size=mask.sizeDelta;
        var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<3;ring++)for(int i=0;i<p.Length;i++)
        {
            Vector2 q=p[i]-new Vector2(.5f,.5f);float expansion=ring==0?1:ring==1?1.12f:1.22f;
            v.Add(new Vector3(position.x+q.x*size.x*expansion,position.y+q.y*size.y*expansion,ring==1?-55:-40));
            uv.Add(UV(hero,new Vector2(259+q.x*302*expansion,264-q.y*348*expansion)));c.Add(White);
            if(ring==0)continue;int k=ring*p.Length+i,n=ring*p.Length+(i+1)%p.Length;t.AddRange(new[]{k,n,n-p.Length,k,n-p.Length,k-p.Length});
        }
        MeshObject(face.name+" raised portrait socket",hero,v,uv,t,c);
    }
    static void DeckWell(string name,float x,float y)
    {
        Quad(name+" darkness",ground,new Rect(x-32,y-94,64,188),new Rect(510,190,5,10),20);
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var c=new List<Color>();
        var ring=Points(-43,-108,43,-108,49,-98,49,98,43,108,-43,108,-49,98,-49,-98);
        for(int band=0;band<3;band++)for(int i=0;i<ring.Length;i++)
        {
            float scale=band==0?1:band==1?.84f:.68f;Vector2 p=ring[i]*scale;
            v.Add(new Vector3(x+p.x,y+p.y,band==2?10:-18));uv.Add(UV(ground,new Vector2(23+band*9,300+(i%4)*42)));
            float shade=band==0?.85f:band==1?1.08f:.34f;c.Add(new Color(shade,shade*.97f,shade*.90f));
            if(band==0)continue;int k=band*ring.Length+i,n=band*ring.Length+(i+1)%ring.Length;t.AddRange(new[]{k,n,n-ring.Length,k,n-ring.Length,k-ring.Length});
        }
        MeshObject(name+" stone mouth and sloped cavity",ground,v,uv,t,c);
    }
    static void AlignPocket(Transform surface,string name,float x,float y)
    {
        var pocket=surface.Find("DeckPockets/"+name);if(pocket==null)return;
        ((RectTransform)pocket).anchoredPosition=new Vector2(x,y);
        foreach(string obsolete in new[]{"StoneRim","Recess"}) {var child=pocket.Find(obsolete);if(child!=null)child.gameObject.SetActive(true);}
        var clip=(RectTransform)pocket.Find("InsidePocket");clip.anchoredPosition=new Vector2(0,0);clip.sizeDelta=new Vector2(58,178);
        foreach(Transform child in clip){var r=(RectTransform)child;r.sizeDelta=new Vector2(70,164);r.anchoredPosition=new Vector2(-5+child.GetSiblingIndex()*3,-3+child.GetSiblingIndex()*2);}
        var lip=pocket.Find("InnerLip");if(lip!=null){((RectTransform)lip).anchoredPosition=new Vector2(-31,0);((RectTransform)lip).sizeDelta=new Vector2(9,183);}
    }
    static void Scenery()
    {
        Cutout("Upper-left rock spire",props,Points(8,249,40,209,88,188,135,221,139,243,114,273,125,301,139,333,164,354,207,361,240,384,236,407,206,428,164,441,132,446,99,427,78,400,65,362,53,327,29,302),p=>new Vector2(-780+(p.x-8)*1.28f,615-(p.y-188)*1.28f),-55,16);
        Cutout("Zeppelin envelope and nose",props,Points(158,154,199,137,207,113,273,112,292,100,342,109,358,122,391,119,415,131,435,142,452,170,467,197,474,231,480,258,506,349,453,312,426,304,392,285,363,301,325,290,299,266,272,241,244,216,228,183,206,172),p=>new Vector2(-635+(p.x-158)*1.13f,610-(p.y-100)*1.13f),-100,38);
        Cutout("Horde fortress",props,Points(580,39,613,12,644,12,664,0,680,31,715,0,730,25,757,18,733,56,757,67,801,83,812,48,832,18,869,23,903,16,918,51,955,32,948,69,1008,48,968,91,1000,113,1008,148,986,167,988,202,966,229,955,266,926,300,886,326,851,319,819,296,807,267,803,237,810,199,812,172,777,140,752,143,739,169,733,208,718,246,685,277,656,279,626,266,618,227,599,205,605,165,595,124,586,96),p=>new Vector2(365+(p.x-580)*1.08f,620-p.y*1.08f),-75,35);
        Cutout("Lower-left cannon",props,Points(1,755,44,746,66,733,78,694,73,650,81,594,97,577,122,637,152,682,167,640,180,623,194,690,226,718,274,723,316,712,361,690,378,704,365,748,345,782,401,774,407,790,366,821,338,840,339,889,359,931,375,982,358,1006,323,986,288,962,252,951,216,966,179,1005,157,1017,148,987,159,945,128,923,87,921,58,943,26,943,34,915,63,891,70,872,37,857,0,843),p=>new Vector2(-850+p.x*1.10f,-190-(p.y-577)*1.00f),-100,35);
        Cutout("Catapult and stacked timber",props,Points(660,798,691,777,725,754,742,694,751,681,777,733,790,707,795,694,813,731,852,717,878,712,906,727,932,702,945,702,953,726,949,761,930,783,909,806,911,837,930,849,944,871,977,860,1002,872,1023,901,1023,1023,1006,1023,984,984,960,960,939,968,908,990,891,1000,862,990,832,969,800,977,789,995,775,1000,768,978,770,939,746,910,716,902,686,887,683,852,657,833),p=>new Vector2(430+(p.x-657)*1.13f,-225-(p.y-681)*.97f),-80,30);
        Cutout("Catapult stone ammunition",props,Points(525,914,540,879,558,854,582,849,599,864,606,889,625,884,641,895,654,907,681,911,694,933,717,940,727,966,718,997,697,1014,658,1021,630,1005,596,1014,574,1003,552,985,532,969),p=>new Vector2(330+(p.x-525)*1.0f,-365-(p.y-849)*.98f),-92,12);
    }
}
