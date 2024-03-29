﻿using Harmonica.Models;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Harmonica.Music
{
	class MediaLocator
	{
		public string? musicPath;
		private FileSystemWatcher watcher;

		public SongFolder rootFolder;
		public event Action? rootFolderChanged;

		private List<string> validExtensions = new List<string>(new string[]
		{
			".m4a",
			".m4b",
			".m4p",
			".wav",
			".aac",
			".flac",
			".mp3",
			".ogg",
			".oga",
			".mogg",
			".webm",
			".aiff",
			".pcm",
			".mp4",
			".mkv",
			".flv",
			".avi",
			".mov",
			".qt",
			".wmv",
			".m4v",
			".mpg"
		});

		public MediaLocator(string? musicPath)
		{
			this.musicPath = musicPath;
			if (!String.IsNullOrWhiteSpace(musicPath))
			{
				Initialize(musicPath);
			}
		}

		public void Initialize(string musicPath)
		{
			this.musicPath = musicPath;

			watcher = new FileSystemWatcher(musicPath, "*.*");
			watcher.IncludeSubdirectories = true;
			// On/Off switch
			watcher.EnableRaisingEvents = true;

			watcher.NotifyFilter =
				NotifyFilters.LastWrite
				| NotifyFilters.FileName
				| NotifyFilters.DirectoryName
				| NotifyFilters.Attributes;

			watcher.Changed += FSW_Event;
			watcher.Created += FSW_Event;
			watcher.Deleted += FSW_Event;
			watcher.Renamed += FSW_Event;

			rootFolder = ScanMusicFolder(musicPath);
			rootFolderChanged?.Invoke();
		}

		public SongFolder ScanMusicFolder(string path)
		{
			SongFolder songFolder = new SongFolder(path);

			foreach (string filePath in Directory.GetFiles(path))
			{
				string extension = Path.GetExtension(filePath);
				if (validExtensions.Contains(extension))
				{
					Song song = new Song(filePath);

					songFolder.Songs.Add(song);
				}
			}
			foreach (string folderPath in Directory.GetDirectories(path))
			{
				songFolder.SongFolders.Add(ScanMusicFolder(folderPath));
			}

			return songFolder;
		}

		/// <summary>
		/// Gets a Media object from the RELATIVE path of the desired song. Absolute song paths are not supported.
		/// </summary>
		/// <param name="libvlc"></param>
		/// <param name="songPath">The relative path to the song on disk.</param>
		/// <returns></returns>
		public Media GetMedia(LibVLC libvlc, string songPath)
		{
			if (musicPath == null) return null;

			string fullFilePath;
			// If there is no '/' or '\' between musicPath and songPath, then we must artificially add a separator
			// between them, so that the two ends (the end and the start for music and song paths respectively) don't get smooshed together
			// WARNING: This might or might not add "C:/" at the start, because thanks Microsoft...
			if (!musicPath.EndsWith('/') && !musicPath.EndsWith('\\') && !songPath.EndsWith('/') && !songPath.EndsWith('\\'))
			{
				fullFilePath = Path.GetFullPath(musicPath + "/"+ songPath);
			}
			else
			{
				fullFilePath = Path.GetFullPath(musicPath + songPath);
			}

			string songUri = new Uri(fullFilePath).AbsoluteUri;

			// If we have a Linux path, simply yank out "//C:/" from the URI
			// Example Windows URI: file:///C:/MySongs/song.mp3
			// Example (Wrong) Linux URI: file:///C:/home/user/songs/song.mp3
			// Example Linux URI: file:/home/user/songs/song.mp3
			if (musicPath.StartsWith('/'))
				songUri = songUri.Replace("//C:/", "");

			return new Media(libvlc, songUri, FromType.FromLocation);
		}

		public Media GetMediaAbsolute(LibVLC libvlc, string absoluteSongPath)
		{
			if (musicPath == null) return null;

			string songUri = new Uri(absoluteSongPath).AbsoluteUri;

			// If we have a Linux path, simply yank out "//C:/" from the URI
			// Example Windows URI: file:///C:/MySongs/song.mp3
			// Example (Wrong) Linux URI: file:///C:/home/user/songs/song.mp3
			// Example Linux URI: file:/home/user/songs/song.mp3
			if (musicPath.StartsWith('/'))
				songUri = songUri.Replace("//C:/", "");

			return new Media(libvlc, songUri, FromType.FromLocation);
		}

		private void FSW_Event(object sender, FileSystemEventArgs e) {
			// TODO: what to do with the old music, especially if it is currently playing, or if a playlist already exists?
			// IDEAS:
			//// Will create the songs, playlists. The MusicPlayer will have REFERENCES to songs (hash in multi, file/song name on single)
			////  that can be de-amiguated here? or somewhere else?
			if (musicPath == null) return;

			rootFolder = ScanMusicFolder(musicPath);
			rootFolderChanged?.Invoke();
		}

	}
}