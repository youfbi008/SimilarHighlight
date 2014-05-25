﻿SimilarHighlight
================

![alt tag](Sample.png?raw=true)

SimilarHighlight is a simple extension by the Managed Extensibility Framework.  

1.Highlight all similar elements by selecting different texts.  
2.The current cursor can be move on to another element of the similar elements by shortcut keys.  

The optional functions:  
3.A margin will be added on the right side of the editor, offer relative position marks about similar elements.
Then you can change the current selected element by selecting marks in the margin.  
4.A pane named "Similar" will be added into the output window, offer more information about similar elements.  
5.In `Tools -> Options -> SimilarHighlight`, you can change some settings or disable some functions.
And in `Fonts and Color -> SimilarHighlight`, you can change the highlighting colors".

This tool has been published on http://visualstudiogallery.msdn.microsoft.com/5b2a1ed1-3514-4786-8a83-a7d82c3a336a.  
This is a introductory video on YouTube. http://youtu.be/6MBXuqFHS2w  (I will update this video.)  

Shortcut Keys:
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
