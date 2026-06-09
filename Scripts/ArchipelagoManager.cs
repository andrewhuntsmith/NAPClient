using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using System;
using Archipelago.MultiClient.Net.Helpers;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Collections.Generic;

namespace NAPClient
{
    internal class ArchipelagoManager
    {
        public static MemorySource MS;
        static ArchipelagoSession ApSession;
        ItemManager ItemManager;
        int CurrentItemIndex = 0;

        public Action<LoginSuccessful> APConnectionEstablished;

        public ArchipelagoManager(MemorySource ms, ItemManager im)
        {
            MS = ms;
            ItemManager = im;
        }

        public bool TryConnect(string url, string slotName, string password)
        {
            CreateAPSession(url);
            RegisterEvents();
            return ConnectToAP(url, slotName, password);
        }

        public void Disconnect()
        {
            Reset();
        }

        void CreateAPSession(string url)
        {
            ApSession = ArchipelagoSessionFactory.CreateSession(url);
        }

        void RegisterEvents()
        {
            ApSession.Socket.ErrorReceived += OnErrorReceived;
            ApSession.Socket.PacketReceived += OnPacketReceived;
            ApSession.Socket.PacketsSent += OnPacketsSent;
            ApSession.Socket.SocketClosed += OnSocketClosed;
            ApSession.Socket.SocketOpened += OnSocketOpened;
            ApSession.Items.ItemReceived += OnItemReceived;
            ApSession.Locations.CheckedLocationsUpdated += OnCheckedLocationsUpdated;
            ApSession.MessageLog.OnMessageReceived += OnMessageReceived;
        }

        // this method pulled almost verbatim from https://archipelagomw.github.io/Archipelago.MultiClient.Net/docs/quick-start.html
        bool ConnectToAP(string server, string slotName, string password)
        {
            LoginResult result;

            try
            {
                result = ApSession.TryConnectAndLogin("N++", slotName, ItemsHandlingFlags.AllItems, null, null, null, password);
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {server} as {slotName}:";
                foreach (string error in failure.Errors)
                {
                    errorMessage += $"\n  {error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    errorMessage += $"\n  {error}";
                }

                return false;
            }

            var loginSuccess = (LoginSuccessful)result;
            APConnectionEstablished.Invoke(loginSuccess);
            return true;
        }

        private static void Reset()
        {
            ApSession = null;
        }

        void OnErrorReceived(Exception e, string message)
        {
            Reset();
        }

        void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            // TODO: Check received packet for information
            return;
        }

        void OnPacketsSent(ArchipelagoPacketBase[] packets)
        {
            // Do I need to do anything here?
            return;
        }

        void OnSocketClosed(string reason)
        {
            Reset();
        }

        void OnSocketOpened()
        {
            // TODO: Log that connection opened
            return;
        }

        void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var newItem = helper.DequeueItem();
                var newItemData = ItemManager.ConvertStringToItem(newItem.ItemName);

                if (ItemManager.Initializing)
                    ItemManager.PreviouslyReceivedItems.Add(newItemData);
                else
                    ItemManager.HandleCondition(newItemData);
            }
        }

        void OnCheckedLocationsUpdated(ReadOnlyCollection<long> newCheckedLocations)
        {
            return;
        }

        void OnMessageReceived(LogMessage message)
        {
            // TODO: Display incoming messages
            return;
        }

        public bool IsConnected() 
        { 
            return ApSession != null; 
        }

        public void SendItem(RandomizationData.CompletionCondition condition)
        {
            long id = 0;
            var stringVersion = "";
            switch (condition.State)
            {
                case ProgressState.LevelComplete:
                    id = ApSession.Locations.GetLocationIdFromName("N++", LogEntry.GenerateLevelName(condition.Id) + " Completion");
                    break;
                case ProgressState.LevelAllGold:
                    id = ApSession.Locations.GetLocationIdFromName("N++", LogEntry.GenerateLevelName(condition.Id) + " Challenge 1 Completion");
                    break;
                case ProgressState.EpisodeComplete:
                    id = ApSession.Locations.GetLocationIdFromName("N++", LogEntry.GenerateEpisodeName(condition.Id) + " Completion");
                    break;
            }
            ApSession.Locations.CompleteLocationChecksAsync(id);
        }
    }
}
