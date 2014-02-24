SimilarHighlight
================

SimilarHighlight is a simple extension by the Managed Extensibility Framework.  

1.Highlight all similar elements by selecting text twice.  
2.The current cursor can be move onto other element of the similar elements by shortcuts.  

You can press `Ctrl + Alt + ->` to make the next similar element selected.  
And, you can press `Ctrl + Alt + <-` to make the the previous similar element selected.  

Now, you can download the extension from Visual Studio Gallery(the url of the next line).  
http://visualstudiogallery.msdn.microsoft.com/5b2a1ed1-3514-4786-8a83-a7d82c3a336a

# Requirements  

* Visual Studio 2012 SDK  

# How to debug

1.Build the solution.  
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.  
3.Set "Command line arguments" of the start options to "/rootsuffix Exp".  

Then, just start running.  
