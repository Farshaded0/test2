using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiScraperApp.Services;

namespace MauiScraperApp.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly RemoteClientService _remoteClient;

    [ObservableProperty] private string _serverIp = "";
    [ObservableProperty] private string _serverPort = "5000";
    [ObservableProperty] private bool _isConnecting;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<string> DiscoveredServers { get; } = new();

    public ConnectionViewModel(RemoteClientService remoteClient)
    {
        _remoteClient = remoteClient;
        var (savedIp, savedPort) = _remoteClient.GetSavedConnectionInfo();
        
        if (!string.IsNullOrEmpty(savedIp))
        {
            ServerIp = savedIp;
            ServerPort = savedPort.ToString();
            StatusMessage = "Last connected: " + savedIp;
        }
        else
        {
            StatusMessage = "Enter PC IP address";
        }
        IsConnected = _remoteClient.IsConnected;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerIp)) return;

        // Clean up input
        string ip = ServerIp.Replace("http://", "").Replace("https://", "").TrimEnd('/');
        if (!int.TryParse(ServerPort, out int port)) port = 5000;

        try
        {
            IsConnecting = true;
            StatusMessage = "Connecting...";
            
            bool success = await _remoteClient.ConnectAsync(ip, port);

            if (success)
            {
                IsConnected = true;
                StatusMessage = $"Connected to {ip}";
                
                // CRITICAL FIX: Navigate to the PAGE ("//SearchView"), not the Container ("//MainTabs")
                await NavigateToMainApp();
            }
            else
            {
                IsConnected = false;
                StatusMessage = "Connection failed";
                await Shell.Current.DisplayAlert("Failed", "Could not connect.", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task ScanNetworkAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "Scanning...";
            DiscoveredServers.Clear();
            List<string> servers = await _remoteClient.DiscoverServersAsync();
            foreach (var server in servers) DiscoveredServers.Add(server);
            StatusMessage = servers.Count > 0 ? $"Found {servers.Count}" : "None found";
        }
        catch { StatusMessage = "Scan Error"; }
        finally { IsScanning = false; }
    }

    [RelayCommand]
    private void SelectServer(string ip)
    {
        ServerIp = ip;
        StatusMessage = $"Selected {ip}";
    }

    [RelayCommand]
    private void Disconnect()
    {
        _remoteClient.Disconnect();
        IsConnected = false;
        StatusMessage = "Disconnected";
    }

    // CHANGED: Must be async Task, not void
    [RelayCommand]
    private async Task ContinueToApp()
    {
        if (IsConnected)
        {
            await NavigateToMainApp();
        }
    }

    // Helper to ensure robust navigation on iOS
    private async Task NavigateToMainApp()
    {
        // Force onto Main Thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try 
            {
                // Small delay to allow button animation to finish (prevents iOS swallowing the input)
                await Task.Delay(100);
                
                // Use absolute route to the SPECIFIC TAB PAGE
                // "///" resets the stack (Good for login flows)
                await Shell.Current.GoToAsync("///SearchView");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Nav Error: {ex.Message}");
                // Fallback: If route fails, this forces the switch
                await Shell.Current.GoToAsync("//MainTabs");
            }
        });
    }
}