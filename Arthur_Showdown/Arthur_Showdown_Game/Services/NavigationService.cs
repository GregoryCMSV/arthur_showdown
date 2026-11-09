using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services
{
    public class NavigationService
    {
        private NavigationManager _navigationManager;
        private LoadingService _loadingService;

        public NavigationService(NavigationManager navigationManager, LoadingService loadingService)
        {
            _navigationManager = navigationManager;
            _loadingService = loadingService;
        }

        public void NavigateTo(string url)
        {
            _loadingService.IniciarCarregamento();
            _navigationManager.NavigateTo(url);
        }

        public void GoToLobby()
        {
            NavigateTo("/lobby");
        }

        public void GoToGame(string gameId)
        {
            NavigateTo($"/combat/{gameId}");
        }

        public void GoToHome()
        {
            NavigateTo("/");
        }
    }
}
