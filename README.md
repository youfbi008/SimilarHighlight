HighlightAndMove
================

HighlightAndMove is a simple extension by the Managed Extensibility Framework.

1.Highlight all similar elements by selecting twice.  
2.The cursor of the selected element can be move onto other element of the similar elements by shortcuts.

You can press `Ctrl + Alt + ->` to make the next similar element selected.  
And, you can press `Ctrl + Alt + <-` to make the the previous similar element selected.

Now, you can download the extension from Visual Studio Gallery(the url of the next line).
http://visualstudiogallery.msdn.microsoft.com/847a1d58-cc6e-4c42-b2a8-54f35af1c7f9

# Requirements

* visual studio 2012 sdk

# How to build

1.Build the solution.  
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.  
3.Set "Command line arguments" of the start options to "/rootsuffix Exp".  

Then, just start running.  
