﻿SimilarHighlight
================

![alt tag](Sample.png?raw=true)

SimilarHighlight is a simple extension by the Managed Extensibility Framework.  

1.Highlight all similar elements by selecting text twice.  
2.The current cursor can be move on to other element of the similar elements by shortcuts.  

The optional functions:
1,A margin weill be added on the right side of the editor, provide relative position marks about similar elements.
Then you can change the current selected element by selecting marks in the margin.  
2,A pane named "Similar" will be added into the output window, provide more information about similar elements.  
3,In `Tools -> Options -> SimilarHighlight`, you can change some settings or disable some functions.

This is a introductory video on YouTube. http://youtu.be/6MBXuqFHS2w(I will update this video.)  

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
