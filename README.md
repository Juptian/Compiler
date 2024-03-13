<h1 align="center" style="position: relative;">
<img width="500" style="border-radius: 50%;" src="UraniumLogo.png" alt="Uranium Logo" /></h1>
<h1 align="center"> Uranium </h1>
<h2 align="center"> 
  <img src="https://img.shields.io/github/workflow/status/Juptian/Uranium/Windows?label=Build&style=plastic"/> 
  <img src="https://img.shields.io/badge/Language-C%23-success?color=success&style=plastic"/> 
  <img src="https://img.shields.io/github/license/Juptian/Uranium?color=success&style=plastic"/>
  <img src="https://codecov.io/gh/Juptian/Uranium/branch/main/graph/badge.svg?token=Y8FO4OE36H"/>
</h2>
Uranium is a compiler that I decided to make from scratch, it is targetted at the CLR and will hopefully output IL code later. Currently, Uranium isn't finished

<h2 align="center"> Contributing </h2>

If you wish to contribute please do the following:
* Fork the project to your own public repo,
* Make the changes and commit them to the forked repo,
* Make a pull request **your own** branch,
* Then, I will personally review the pull request, I will give feedback on it, and then merge or decline it.

<h2 align="center"> Issues </h2>

If you have issues, please do the following:
* Create an issue with a short, yet descriptive title
* In the issue, please explain what the issue is in depth and how to recreate it

Finally, please be patient as I try to fix it. If it's not fixed quickly, you can always contribute to the repo by making a PR for it

<h2 align="center"> Not understand a part of the code? </h2>
Feel free to open an issue asking about it, asking for specifications or asking me to write a `README` about it, I'll be more than happy to respond!

<h2 align="center"> Using & Installing </h2>

To use the compiler, please make sure you have .NET 5 installed, once you've done that, do the following steps:
* Add text to the ` test.urnm ` file located in the central folder
* Either run the ` run.bat ` file I have in that same directory, or run it via command line with ` dotnet run test.txt `

<h3 align="center"> Additional arguments: </h3>
The current command line arguments accepted are:

```
--showtree
```
This will show the syntax tree that is produced.

```
--boundTree
```
This will show the bound syntax tree that is produced

<h3 align="center"> How to use additional arguments: </h3>
To use additional arguments, simply type 

```
dotnet run test.txt
``` 
 
then add each argument, separated by a space, so for example:

``` 
dotnet run test.txt --showtree 
```


<h3 align="center">This repository is a part of the <b><a href="https://github.com/salty-sweet/TLoDLiBSsf">The List of Developing Languages in Brackeys Server so far</a> I recommend you go check out the others</h4>
