using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Model;
using VkNet.Model.Attachments;

namespace VOffline.Models.Storage
{
    public class AlbumWithPhoto
    {
        public PhotoAlbum Album { get; }
        public IReadOnlyList<Photo> Photo { get; }

        public AlbumWithPhoto(PhotoAlbum album, IReadOnlyList<Photo> photo)
        {
            Album = album;
            Photo = photo
                .OrderBy(a => a.CreateTime)
                .ToList();
        }

        public AlbumWithPhoto(Album album, IReadOnlyList<Photo> photo)
        {
            this.Album = new PhotoAlbum
            {
                Id = album.Id.Value,
                Title = album.Title,
                Description = album.Description,
                Created = album.CreateTime,
                ThumbId = album.Thumb?.Id,
                OwnerId = album.OwnerId,
                Updated = album.UpdateTime
            };
            this.Photo = photo
                .OrderBy(a => a.CreateTime)
                .ToList();
        }

        public AlbumWithPhoto(IReadOnlyList<Photo> photo)
        {
            this.Album = new PhotoAlbum
            {
                Id = long.MinValue,
                Title = "__default",
                Description = "Photos without album",
                Created = DateTime.MinValue
            };
            this.Photo = photo
                .OrderBy(a => a.CreateTime)
                .ToList();
        }

        
    }
}