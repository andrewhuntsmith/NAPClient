using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using System;
using Archipelago.MultiClient.Net.Helpers;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.MessageLog.Messages;

namespace NAPClient
{
    internal class ArchipelagoManager
    {
        public static MemorySource MS;
        ArchipelagoSession ApSession;
        ItemManager ItemManager;
        int CurrentItemIndex = 0;

        public ArchipelagoManager(MemorySource ms, ItemManager im)
        {
            MS = ms;
            ItemManager = im;
        }

        public void TryConnect(string url, string slotName, string password)
        {
            CreateAPSession(url);
            RegisterEvents();
            ConnectToAP(url, slotName, password);
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
        void ConnectToAP(string server, string slotName, string password)
        {
            LoginResult result;

            try
            {
                result = ApSession.TryConnectAndLogin("N++", slotName, ItemsHandlingFlags.AllItems);
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

                return;
            }

            var loginSuccess = (LoginSuccessful)result;
        }

        void OnErrorReceived(Exception e, string message)
        {
            throw new NotImplementedException();
        }

        void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            throw new NotImplementedException();
        }

        void OnPacketsSent(ArchipelagoPacketBase[] packets)
        {
            throw new NotImplementedException();
        }

        void OnSocketClosed(string reason)
        {
            throw new NotImplementedException();
        }

        void OnSocketOpened()
        {
            throw new NotImplementedException();
        }

        void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var newItem = helper.DequeueItem();
                var newItemData = ItemManager.ConvertStringToItem(newItem.ItemName);
                ItemManager.HandleCondition(newItemData);
                // TODO: display received item in client
            }
        }

        void OnCheckedLocationsUpdated(ReadOnlyCollection<long> newCheckedLocations)
        {
            throw new NotImplementedException();
        }

        void OnMessageReceived(LogMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
