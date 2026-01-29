# OpenTK Lighting

GitHub doesn't save models, so if you want to load this on your computer, use this link: https://drive.google.com/file/d/1YqhS0SpB4NTV52VaMRsN8vQzu-VGJzTA/view?usp=sharing. 

This project is built with **OpenTK** and focuses on lighting, shaders, and resource management.  
Please do not copy or reuse the code directly. If you find it helpful, feel free to learn from it or adapt ideas with attribution. (Though the code might be a bit messy and unoptimized)

The scene has three lights, one red, one green, and one blue. That is what's making the chromatic aberration look.
<img width="1919" height="1079" alt="Screenshot 2026-01-29 135609" src="https://github.com/user-attachments/assets/4442cda7-6c12-4980-9810-c8591a4ffe57" />

<img width="1919" height="1079" alt="Screenshot 2026-01-29 135717" src="https://github.com/user-attachments/assets/f8ac4dda-8a2e-4f03-b6a4-a26b78cfdb3b" />

<img width="1919" height="1079" alt="Screenshot 2026-01-29 135717" src="https://github.com/user-attachments/assets/db073f1c-3c62-4009-87d6-7755a48ced67" />


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
