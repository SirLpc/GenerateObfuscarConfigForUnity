# GenerateObfuscarConfigForUnity
A unity engine tool for generate general [Obfuscar](https://github.com/obfuscar/obfuscar) xml config.

The main purpose of this tool is to make sure your unity c# code work properly after confused by Obfuscar.

# Usage
1. Place ObfuscarConfigInfo.cs in your project.
2. Create a scriptable object of ObfuscarConfigInfo.
3. Set up some basic things. Specially, you should adapt the paths to yours in the purple box below:
![image](https://user-images.githubusercontent.com/17614861/203248780-5ca1355d-a72f-45bb-9340-7119bcb36517.png)
4. Now you can generate the config or do Obfuscar from the context menu.
![image](https://user-images.githubusercontent.com/17614861/203249065-7ed68af6-e5cd-4c32-98f8-9e7f9758ed16.png)
5. If all goes will, you should get a config like this:
![image](https://user-images.githubusercontent.com/17614861/203249996-5d547435-0dff-4590-94e6-ae86873ed81a.png)

# A sample project
https://github.com/SirLpc/GenerateObfuscarConfigForUnity_sample

# Notice
You should pay attention to those name sensitve members in your class, such as a method will be used by unity delay Invoke("NameOfMethod"). At this point, you could create a custom attribule to mark a method should not be renamed, and config this attribute in the `ObfuscarModuleIgnoreAttributes` array in the ObfuscarConfigInfo config before.

### At last, great thanks for the author of [Obfuscar](https://github.com/obfuscar/obfuscar) project :)

