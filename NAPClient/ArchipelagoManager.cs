using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using System;

namespace NAPClient
{
    internal class ArchipelagoManager
    {
        public static MemorySource MS;
        ArchipelagoSession apSession;

        public ArchipelagoManager (MemorySource ms)
        {
            MS = ms;
        }

        public void TryConnect(string url, string slotName, string password)
        {
            CreateAPSession(url);
            ConnectToAP(url, slotName, password);
        }

        void CreateAPSession(string url)
        {
            apSession = ArchipelagoSessionFactory.CreateSession(url);
        }

        // this method pulled almost verbatim from https://archipelagomw.github.io/Archipelago.MultiClient.Net/docs/quick-start.html
        void ConnectToAP(string server, string slotName, string password)
        {
            LoginResult result;

            try
            {
                result = apSession.TryConnectAndLogin("N++", slotName, ItemsHandlingFlags.AllItems);
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
    }
}
