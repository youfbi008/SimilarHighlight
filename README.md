﻿SimilarHighlight
================

![Extension sample](https://github.com/youfbi008/SimilarHighlight/blob/master/PreviewCsharp.jpg?raw=true)

SimilarHighlight is a simple extension by the Managed Extensibility Framework.  

1.Highlight all similar elements by selecting text twice.  
2.The current cursor can be move onto other element of the similar elements by shortcuts.  

And a pane named "Similar" will be added into the output window, then you can check some information in there.

This is a introductory video on YouTube. http://youtu.be/6MBXuqFHS2w  

ShortKeys:
* `Ctrl + Alt + ->` to make the next similar element selected.  
* `Ctrl + Alt + <-` to make the previous similar element selected.  
* `ESC` to make the highlighted elements return to normal.  

# Supported Languages  

* C(.c)  
* C#(.cs)  
* JAVA(.java)  
* JavaScript(.js)  
※The operations in the next languages will not be perfect. I will fix them in future.
* Python(.py)  
* Ruby(.rb)  
* Cobol(.cbl) Now some elements can be highlighted. You can test it use CobolTest.CBL.  

# Requirements  

* NuGet You can install NuGet Package Manager with Extension Manager.
* Visual Studio 2012 SDK  

# How to debug

1.Build the solution.  
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.  
3.Set "Command line arguments" of the start options to "/rootsuffix Exp".  

Then, just start running.  
