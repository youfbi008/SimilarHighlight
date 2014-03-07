SimilarHighlight
================

SimilarHighlight is a simple extension by the Managed Extensibility Framework.  

1.Highlight all similar elements by selecting text twice.  
2.The current cursor can be move onto other element of the similar elements by shortcuts.  

Take a look at this picture, you will know what happened.
https://github.com/youfbi008/SimilarHighlight/blob/master/preview.jpg

ShortKeys:
* `Ctrl + Alt + ->` to make the next similar element selected.  
* `Ctrl + Alt + <-` to make the previous similar element selected.  
* `ESC` to make the highlighted elements return to normal.  

# Supported Languages  

* C#(.cs)  
* JAVA(.java)  
We will try to make it support other languages in future.  

# Requirements  

* NuGet You can install NuGet Package Manager with Extension Manager.
* Visual Studio 2012 SDK  

# How to debug

1.Build the solution.  
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.  
3.Set "Command line arguments" of the start options to "/rootsuffix Exp".  

Then, just start running.  
