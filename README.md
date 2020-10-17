<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/VladTheJunior/Resource-Manager">
    <img src="Images/Icon.png" alt="Logo" width="128" height="128">
  </a>

  <h3 align="center">Resource Manager</h3>

  <p align="center">
    Utility for viewing, comparing, creating and extracting files from Age of Empires III .BAR archive  </p>
     <h1 align="center"><a href="https://drive.google.com/file/d/15-LyNy613JMMVV8xjRLFPG8Xqp88aHOh/view?usp=sharing">Download Portable (.zip archive)</a></h1>

</p>



<!-- TABLE OF CONTENTS -->
## Table of Contents

* [How to Use](#how-to-use)
* [About the Project](#about-the-project)
* [Screenshots](#screenshots)
* [License](#license)
* [Contact](#contact)
* [Acknowledgements](#acknowledgements)

## How to Use

1. Download application from the link above.
2. Unpack .zip and run *Resource Manager.exe*.
3. Application requires .NET Core. If you haven't installed it yet, you can download it from the direct link: [.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.9-windows-x64-installer).

<!-- ABOUT THE PROJECT -->
## About The Project

I would like to present to you a program for viewing, comparing, creating and extracting files from BAR archives of the Age of Empires 3: Definitive Edition (also supports AoE3 2007). This tool replaces the AoE3Ed Viewer developed by Ykkrosh, which does not work for the Definitive Edition.
The updated version includes all **(x)** functions that were in AoE3Ed Viewer, as well as new features:

**Preview:**
* Syntax highlighting in previewing text files (xml, xs).
* Search in preview in text files (CTRL + F).
* Ability to scale images in preview.

**Entries table:**
* Grouping files by their format (optional).
* Sort by name, size, creation date.
* Search in the BAR archive.
* Calculation of CRC (optional).
* Create BAR archive from files and folders.
* The size of the selected entries.

**Conversion:**
* Converting XML <-> XMB (both 2007 and DE).
* Converting DDT -> PNG.

**Extract:**
* Extract all files.
* Extract selected files.

**Other:**
* Comparison of BAR archives.


**(x) Currently the application does not include the following features:**
* Converting DDT <-> TGA (it is recommended to use [Photoshop Plugin by kangcliff](http://aoe3.heavengames.com/cgi-bin/forums/display.cgi?action=ct&f=14,39229,,10)).
* Preview and correct unpacking of sound files (files are encrypted, I will be grateful to the developers if they give more detailed information on this).

## Screenshots

![](Images/1.PNG)
![](Images/2.PNG)
![](Images/3.PNG)


<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.



<!-- CONTACT -->
## Contact

VladTheJunior - Discord: VladTheJunior#1244 - VladTheJunior@gmail.com

Project Link: [https://github.com/VladTheJunior/Resource-Manager](https://github.com/VladTheJunior/Resource-Manager)



<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
* [ProjectCeleste/ProjectCeleste.GameFiles.Tools](https://github.com/ProjectCeleste/ProjectCeleste.GameFiles.Tools)
* [PaulZero/AoE3-File-Readers](https://github.com/PaulZero/AoE3-File-Readers)
* [AoE3Ed by Ykkrosh](http://games.build-a.com/aoe3/files/)
