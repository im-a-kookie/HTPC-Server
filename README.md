# MediaServer

WIP rebuild of HTPC to fix some general badness in the old one.

Implements HTTP protocol over TCP Listener for delivering media files and a simple website interface, and exposes an Avalonia window, hosted by the server, for viewing the library and media files on a local HTPC.

Written in C#.NET. Web interface requires inclusion of video-js library in wwwroot within the server/host path.
