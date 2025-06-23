# OpenTK Lighting

This project is built with **OpenTK** and focuses on lighting, shaders, and resource management.  
Please do not copy or reuse the code directly. If you find it helpful, feel free to learn from it or adapt ideas with attribution. (Though the code might be a bit messy and unoptimized)

## 📦 Project Structure

### 🔄 Loaders
Scripts for loading mesh or image data into the "engine".  
Image.cs just has a function called `loadTexture` you give it a png file path and it will return the texture handle. (I think of it more as an ID)  
OBJ_Parser.cs just has a function called `ParseOBJFile` you give it an OBJ file path and it will return 
`(List<float> Vertices,List<uint> Indices,List<float> TextureCoordinants,List<float> Normals)`. Vertices in a format like x,y,z,x,y,z... And Indices are faces t(v1,v2,v3),t(v1,v2,v3)... (TextureCoords and Normals are per vertex)

---

### 🎨 Shaders
Shaders are organized in the following format:  
```
─┬─ Shaders  
 └─┬─ ShaderName  
   ├─── vertex.glsl  
   └─── fragment.glsl  
```

### 🧱 Objects
Object data is structured as:
```
─┬─ Objects  
 └─┬─ ObjectName  
   └─┬─ Textures  
     ├─── color.png  
     ├─── normal.png  
     └─── specular.png  
```

## 📁 Example Directory Layout
```
OpenTK Lighting  
├─┬─ Loaders  
│ ├─── OBJ_Parser.cs  
│ └─── Image.cs  
├─┬─ Objects  
│ └─┬─ Cube  
│   └─┬─ Textures  
│     ├─── color.png  
│     ├─── normal.png  
│     └─── specular.png  
├─┬─ Shaders  
│ └─┬─ Base  
│   ├─── vertex.glsl  
│   └─── fragment.glsl  
```

---

## 📝 Notes

- Test Formating A;
- Test Formating B;
- Test Formating C;
- Oh and also this uses ImGUI.Net