# OpenTK Lighting

This project is built with **OpenTK** and focuses on lighting, shaders, and resource management.  
Please do not copy or reuse the code directly. If you find it helpful, feel free to learn from it or adapt ideas with attribution.

## 📦 Project Structure

### 🔄 Loaders
Scripts for loading mesh or image data into the "engine".

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

---

## 📝 Notes

- Shaders must follow the directory format exactly for automatic loading.
- Objects require at least a `color.png` texture to be valid.

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