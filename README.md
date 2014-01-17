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


