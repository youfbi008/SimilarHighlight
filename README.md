## Overview

OutputTextSeletion is an Addin for Visual Studio that lets you conveniently select output text of "WriteLine" method.

You can press `Ctrl + Alt + ->` to select the output text of the next "WriteLine" method.  
And, you can press `Ctrl + Alt + <-` to select the output text of the previous "WriteLine" method.

For example,
```C#
Console.WriteLine("abcde");
DoSomething();
Debug.WriteLine("hijkl");
```
when the cursor between the two WriteLine method,  
if you press `Ctrl + Alt + <-`, then the whole text of "abcde" will be selected.

# How to build

Please move the file named "OutputTextSeletionForDoc.AddIn" to the "Addins" folder of Visual Studio
within your Windows Documents folder.
For example, under Windows 7 and Visual Studio 2012. The path below is correct.
`C:\Users\<user name>\Documents\Visual Studio 2012\Addins\OutputTextSeletionForDoc.AddIn`

Then you need to edit the file, and change the path of library to the location of your files.
`<Assembly>D:\DevelopWorks\OutputTextSeletion\OutputTextSeletion\bin\OutputTextSeletion.dll</Assembly>`

Finally, I think you can build the project and debug it.
