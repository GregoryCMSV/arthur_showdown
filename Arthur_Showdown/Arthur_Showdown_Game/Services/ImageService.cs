using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services
{
    public class ImageService
    {
        private string _imagePath = "Images/";
        private string _backgroundPath;

        public ImageService()
        {
            _backgroundPath = $"{_imagePath}Background/";
        }

        public string ObterBackgroundLogin() => $"{_backgroundPath}BGLogin.jpg";
    }
}
