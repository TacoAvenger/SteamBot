using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class EarbudUserHandler : UserHandler
    {

        static int SellPricePerKey = 20;
        static int BuyPricePerKey = 19;
        static int InviteTimerInterval = 2000;

        int UserKeysAdded, UserBudsAdded, BotBudsAdded, BotKeysAdded, InventoryKeys, InventoryBuds, OverpayNumKeys, PreviousKeys, WhileLoop, InvalidItem = 0;

        double ExcessRefined = 0.0;

        bool InGroupChat, TimerEnabled, HasRun, HasErrorRun, ChooseDonate, AskOverpay, IsOverpaying, HasCounted = false, isGifted;
        bool TimerDisabled = true;

        ulong uid;
        SteamID currentSID;

        Timer inviteMsgTimer = new System.Timers.Timer(InviteTimerInterval);

        public EarbudUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
        }
        public override bool OnGroupAdd()
        {
            return false;
        }
        public override void OnLoginCompleted()
        {
        }
        public override void OnTradeSuccess()
        {
        }
        public override bool OnFriendAdd()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            inviteMsgTimer.Interval = InviteTimerInterval;
            inviteMsgTimer.Elapsed += (sender, e) => OnInviteTimerElapsed(sender, e, EChatEntryType.ChatMsg);
            inviteMsgTimer.Enabled = true;
            return true;
        }

        public override void OnFriendRemove()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") removed me!");
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            message = message.ToLower();

            //REGULAR chat commands
            if (message.Contains("buying") || message.Contains("what") || message.Contains("how many") || message.Contains("how much") || message.Contains("price") || message.Contains("selling"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "I buy earbuds for " + String.Format("{0:0}", (BuyPricePerKey)) + " keys, and sell earbuds for " + String.Format("{0:0}", (SellPricePerKey)) + " keys.");
            }
            else if ((message.Contains("love") || message.Contains("luv") || message.Contains("<3")) && (message.Contains("y") || message.Contains("u")))
            {
                if (message.Contains("do"))
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "I love you lots. <3");
                }
                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "I love you too!");
                }
            }
            else if (message.Contains("<3"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "<3");
            }
            else if (message.Contains("fuck") || message.Contains("suck") || message.Contains("dick") || message.Contains("cock") || message.Contains("tit") || message.Contains("boob") || message.Contains("pussy") || message.Contains("vagina") || message.Contains("cunt") || message.Contains("penis"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Sorry, but as a robot I cannot perform sexual functions.");
            }
            else if (message.Contains("thank"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "You're welcome!");
            }
            else if (message == "donate")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Please type that command into the TRADE WINDOW, not here! And thanks. <3");
            }
            else if (message == "buy")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "That's an old command, and is unnecessary. Just trade me to begin!");
            }
            else if (message == "sell")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "That's an old command, and is unnecessary. Just trade me to begin!");
            }
            else if (message.Contains("help"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Hi. Thanks for using The CTS Community's keybanking bot! Trade me, then simply put up your keys or metal and I will add my keys or metal automatically. I also accept donations of either keys or metal. To donate, type \"donate\" in the trade window!");
            }
            // ADMIN commands
            else if (IsAdmin)
            {
                if (message.StartsWith(".join"))
                {
                    // Usage: .join GroupID - e.g. ".join 103582791433582049" or ".join cts" - this will allow the bot to join a group's chatroom
                    if (message.Length >= 7)
                    {
                        if (message.Substring(6) == "tf2")
                        {
                            uid = 103582791430075519;
                        }
                        else
                        {
                            ulong.TryParse(message.Substring(6), out uid);
                        }
                        var chatid = new SteamID(uid);
                        Bot.SteamFriends.JoinChat(chatid);
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Joining chat: " + chatid.ConvertToUInt64().ToString());
                        InGroupChat = true;
                        Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
                        Bot.log.Success("Joining chat: " + chatid.ConvertToUInt64().ToString());
                    }
                }
                else if (message.StartsWith(".leave"))
                {
                    // Usage: .leave GroupID, same concept as joining
                    if (message.Length >= 8)
                    {
                        if (message.Substring(7) == "tf2")
                        {
                            uid = 103582791430075519;
                        }
                        else
                        {
                            ulong.TryParse(message.Substring(7), out uid);
                        }
                        var chatid = new SteamID(uid);
                        Bot.SteamFriends.LeaveChat(chatid);
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Leaving chat: " + chatid.ConvertToUInt64().ToString());
                        InGroupChat = false;
                        Bot.log.Success("Leaving chat: " + chatid.ConvertToUInt64().ToString());
                    }
                }
                else if (message.StartsWith(".sell"))
                {
                    // Usage: .sell newprice "e.g. sell 26"
                    int NewSellPrice = 0;
                    if (message.Length >= 6)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Current selling price: " + SellPricePerKey + " keys.");
                        int.TryParse(message.Substring(5), out NewSellPrice);
                        Bot.log.Success("Admin has requested that I set the new selling price from " + SellPricePerKey + " keys to " + NewSellPrice + " keys.");
                        SellPricePerKey = NewSellPrice;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Setting new selling price to: " + SellPricePerKey + " keys.");
                        Bot.log.Success("Successfully set new price.");
                    }
                    else
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "I need more arguments. Current selling price: " + SellPricePerKey + " keys.");
                    }
                }
                else if (message.StartsWith(".buy"))
                {
                    // Usage: .buy newprice "e.g. .buy 24"
                    int NewBuyPrice = 0;
                    if (message.Length >= 5)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Current buying price: " + BuyPricePerKey + " keys.");
                        int.TryParse(message.Substring(4), out NewBuyPrice);
                        Bot.log.Success("Admin has requested that I set the new selling price from " + BuyPricePerKey + " keys to " + NewBuyPrice + " keys.");
                        BuyPricePerKey = NewBuyPrice;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Setting new buying price to: " + BuyPricePerKey + " keys.");
                        Bot.log.Success("Successfully set new price.");
                    }
                    else
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "I need more arguments. Current buying price: " + BuyPricePerKey + " keys.");
                    }
                }
                else if (message.StartsWith(".gmessage"))
                {
                    // usage: say ".gmessage Hello!" to the bot will send "Hello!" into group chat
                    if (message.Length >= 10)
                    {
                        if (InGroupChat)
                        {
                            var chatid = new SteamID(uid);
                            string gmessage = message.Substring(10);
                            Bot.SteamFriends.SendChatRoomMessage(chatid, type, gmessage);
                            Bot.log.Success("Said into group chat: " + gmessage);
                        }
                        else
                        {
                            Bot.log.Warn("Cannot send message because I am not in a group chatroom!");
                        }
                    }
                }
                else if (message == ".canceltrade")
                {
                    // Cancels the trade. Occasionally the message will be sent to YOU instead of the current user. Oops.
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(currentSID, EChatEntryType.ChatMsg, "My creator has forcefully cancelled the trade.");
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
            }
        }

        public override bool OnTradeRequest()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") has requested to trade with me!");
            return true;
        }

        public override void OnTradeError(string error)
        {
            Bot.log.Warn(error);
            if (!HasErrorRun)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "If the bot bugged out, please leave a message on my profile saying what happened.");
                HasErrorRun = true;
            }
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void OnTradeTimeout()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were either AFK or took too long and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void OnTradeInit()
        {
            ReInit();
            TradeCountInventory(true);
            Trade.SendMessage("Welcome!  Place your keys or earbuds in the trade window to begin!");
            if (InventoryBuds == 0)
            {
                Trade.SendMessage("I don't have any earbuds to sell right now! I am currently buying earbuds for " + String.Format("{0:0}", (BuyPricePerKey)) + " keys.");
            }
            else if (InventoryKeys < BuyPricePerKey)
            {
                Trade.SendMessage("I don't have enough keys to buy earbuds! I am selling earbuds for " + String.Format("{0:0}", (SellPricePerKey)) + " keys.");
            }
            else
            {
                Trade.SendMessage("I am currently buying earbuds for " + String.Format("{0:0}", (BuyPricePerKey)) + " keys, and selling earbuds for " + String.Format("{0:0}", (SellPricePerKey)) + " keys.");
            }
            Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            if (!HasCounted)
            {
                Trade.SendMessage("ERROR: I haven't finished counting my inventory yet! Please remove any items you added, and then re-add them or there could be errors.");
            }
            else if (InvalidItem >= 4)
            {
                Trade.CancelTrade();
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "U think ur funny m8?");
                Bot.log.Warn("Booted user for messing around.");
                Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            }
            else if (item.Defindex == 5021)
            {
                // Put up keys
                UserKeysAdded++;
                Bot.log.Success("User added: " + item.ItemName);
            }
            else if (item.Defindex == 143)
            {

                int size = inventoryItem.Attributes.Length;
                for (int count = 0; count < size; count++)
                {
                    if (inventoryItem.Attributes[count].Defindex == 186)
                    {
                        isGifted = true;

                        Bot.log.Warn("User added gifted earbuds");
                        InvalidItem++;
                        break;
                    }
                    else
                    {
                        isGifted = false;
                    }
                }


                // Put up buds
                if (isGifted == false)
                {
                    UserBudsAdded++;
                    Bot.log.Success("User added: " + item.ItemName);
                }
                else
                {
                    Trade.SendMessage("I don't accept gifted earbuds.  Remove them from the trade to continue...");
                }

                // USER IS SELLING EARBUDS
                if (!ChooseDonate)
                {
                    // BOT ADDS KEYS
                    int KeysToScrap = UserBudsAdded * BuyPricePerKey;
                    if (InventoryKeys < BuyPricePerKey)
                    {
                        Trade.SendMessage("I only have " + InventoryKeys + " keys left. You need to remove some earbuds.");
                        Bot.log.Warn("I don't have enough keys for the user.");
                    }
                    else
                    {
                        Trade.SendMessage("You have given me " + UserBudsAdded + " earbud(s). I will give you " + KeysToScrap + " keys.");
                        Bot.log.Success("User gave me " + UserBudsAdded + " earbud(s). I will now give him " + KeysToScrap + " keys.");
                        // Put up required metal
                        bool DoneAddingMetal = false;
                        while (!DoneAddingMetal)
                        {
                            if (InventoryKeys > 0 && BotKeysAdded < KeysToScrap && !isGifted)
                            {
                                Trade.AddItemByDefindex(5021);
                                Bot.log.Warn("I added 1 key.");
                                BotKeysAdded++;
                                InventoryKeys--;
                            }
                            else if (BotKeysAdded == KeysToScrap)
                            {
                                Trade.SendMessage("Added " + BotKeysAdded + " keys.");
                                Bot.log.Success("Gave user enough keys!");
                                DoneAddingMetal = true;
                            }
                        }
                    }
                }
            }
            else
            {
                // Put up other items
                Trade.SendMessage("Sorry, I only accept earbuds/keys. Please remove any other items from the trade to continue.");
                Bot.log.Warn("User added:  " + item.ItemName);
                InvalidItem++;
            }
            // USER IS BUYING EARBUDS
            if (!ChooseDonate)
            {
                if (UserKeysAdded % SellPricePerKey == 0 && UserKeysAdded >= SellPricePerKey)
                {
                    // Count refined and convert to keys -- X scrap per key
                    int NumKeys = UserKeysAdded / SellPricePerKey;
                    if (NumKeys > 0 && NumKeys > PreviousKeys && NumKeys <= (BotBudsAdded + InventoryBuds))
                    {
                        Trade.SendMessage("You put up enough keys for " + NumKeys + " earbud(s). Adding your earbuds now...");
                        Bot.log.Success("User put up enough keys for " + NumKeys + " earbud(s).");
                        if (InventoryBuds == 0)
                        {
                            double excess = ((NumKeys - BotBudsAdded) * SellPricePerKey);
                            string refined = string.Format("{0}", excess);
                            Trade.SendMessage("I only have " + BotBudsAdded + " earbuds.  Please remove " + refined + " key(s).");
                           
                        }
                        else
                        {
                            // Add the keys to the trade window
                            for (int count = BotBudsAdded; count < NumKeys; count++)
                            {
                                Trade.AddItemByDefindex(143);
                                Bot.log.Warn("I am adding 1 Earbud.");
                                BotBudsAdded++;
                                InventoryBuds--;

                            }
                            Trade.SendMessage("I have added " + BotBudsAdded + " earbud(s) for you.");
                            Bot.log.Success("I have added " + BotBudsAdded + " earbud(s) for the user.");
                            PreviousKeys = NumKeys;
                        }
                    }
                    else
                    {
                        Trade.SendMessage("You put up enough keys for " + NumKeys + " earbuds, but I only have " + BotBudsAdded + " earbud(s).");
                        Bot.log.Warn("User wanted to buy " + NumKeys + " earbud(s), but I only have " + BotBudsAdded + " earbud(s).");
                    }
                }
            }
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            if (item.Defindex == 5021)
            {
                // Removed keys
                UserKeysAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 143 && isGifted == false)
            {
                // Removed earbuds
                UserBudsAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 143 && isGifted == true)
            {
                Bot.log.Success("User removed gifted earbuds");
            }
            else
            {
                // Removed other items
                Bot.log.Warn("User removed: " + item.ItemName);
            }
            // User removes key from trade
            if (UserBudsAdded < (float)BotKeysAdded / BuyPricePerKey)
            {
                int KeysToScrap = UserBudsAdded * BuyPricePerKey;
                bool DoneAddingMetal = false;
                while (!DoneAddingMetal)
                {
                    WhileLoop++;
                    if (BotKeysAdded > 0 && BotKeysAdded >= KeysToScrap)
                    {
                        Trade.RemoveItemByDefindex(5021);
                        Bot.log.Warn("I removed 1 Key.");
                        BotKeysAdded--;
                        InventoryKeys++;
                    }
                    else if (BotKeysAdded == KeysToScrap)
                    {
                        DoneAddingMetal = true;
                    }
                    else if (WhileLoop > 50)
                    {
                        Trade.SendMessage("Error: I could not remove the proper amounts of keys from the trade. I might be out of keys - try adding more earbuds if possible, or remove a few earbuds.");
                        WhileLoop = 0;
                        DoneAddingMetal = true;
                        break;
                    }
                }
            }
            // User removes metal from trade
            while ((float)UserKeysAdded / SellPricePerKey < BotBudsAdded)
            {
                    Trade.RemoveItemByDefindex(143);
                    Bot.log.Warn("I removed 1 Earbud");
                    BotBudsAdded--;
                    InventoryBuds++;
                    PreviousKeys = BotBudsAdded;
                    IsOverpaying = false;
            }
        }

        public override void OnTradeMessage(string message)
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            message = message.ToLower();

            if (message == "donate")
            {
                ChooseDonate = true;
                Trade.SendMessage("Oh, you want to donate earbuds or keys? Thank you so much! Please put up your items and simply click \"Ready to Trade\" when done! If you want to buy or sell earbuds again you need to start a new trade with me.");
                Bot.log.Success("User wants to donate!");
            }
            else if (message == "continue")
            {
                if (AskOverpay)
                {
                    IsOverpaying = true;
                    Trade.SendMessage("You have chosen to continue overpaying. Click \"Ready to Trade\" again to complete the trade.");
                    Bot.log.Warn("User has chosen to continue overpaying!");
                }
                else
                {
                    Trade.SendMessage("You cannot use this command right now!");
                    Bot.log.Warn("User typed \"continue\" for no reason.");
                }
            }

        }

        public override void OnTradeReady(bool ready)
        {
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                Bot.log.Success("User is ready to trade!");
                if (Validate())
                {
                    Trade.SetReady(true);
                }
                else
                {
                    if (AskOverpay && OverpayNumKeys != 0 && !ChooseDonate)
                    {
                        double AdditionalRefined = (SellPricePerKey) - ExcessRefined;
                        string addRef = string.Format("{0:}", AdditionalRefined);
                        string refined = string.Format("{0:}", ExcessRefined);
                        Trade.SendMessage("Remove " + refined + " key(s), or add " + addRef + " key(s). You cannot complete the trade unless you do so.");
                        Bot.log.Warn("User has added an excess of " + refined + " keys. He can add " + addRef + " keys for another earbud. Asking user if they want to continue.");
                        
                    }
                    else
                    {
                        ResetTrade(false);
                    }
                }
            }
        }

        public override void OnTradeAccept()
        {
            if (Validate() || IsAdmin)
            {
                bool success = Trade.AcceptTrade();
                if (success)
                {
                    Log.Success("Trade was successful!");
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Thanks for a successful trade!  Got comments or suggestions?  Leave a message on my profile!");
                    Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                    Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
                }
            }
            OnTradeClose();
        }

        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            base.OnTradeClose();
        }

        public bool Validate()
        {
            int KeyCount = 0;
            int BudCount = 0;

            List<string> errors = new List<string>();

            foreach (ulong id in Trade.OtherOfferedItems)
            {
                var item = Trade.OtherInventory.GetItem(id);
                var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                if (item.Defindex == 5021)
                {
                    KeyCount++;
                }
                else if (item.Defindex == 143 && isGifted == false)
                {
                    BudCount++;
                }
                else if (item.Defindex == 143 && isGifted == true)
                {
                    errors.Add("I can't accept gifted earbuds");
                }
                else
                {
                    errors.Add("I can't accept " + schemaItem.ItemName + "!");
                }
            }

            if (ChooseDonate)
            {
                foreach (ulong id in Trade.OtherOfferedItems)
                {
                    var item = Trade.OtherInventory.GetItem(id);
                    var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                    if (schemaItem.ItemName != "Mann Co. Supply Crate Key" && schemaItem.ItemName != "#TF_Tool_DecoderRing" && item.Defindex != 143)
                    {
                        errors.Add("I'm sorry, but I cannot accept " + schemaItem.ItemName + "!");
                    }
                }

                if (BotKeysAdded > 0 || BotBudsAdded > 0)
                {
                    errors.Add("You can't do that :( I still have items put up!");
                }
            }
            else if (UserBudsAdded > 0)
            {
                Bot.log.Warn("User has " + BudCount + " earbud(s) put up. Verifying if " + (float)BotKeysAdded / BuyPricePerKey + " == " + BudCount + ".");
                if (BudCount != (float)BotKeysAdded / BuyPricePerKey)
                {
                    errors.Add("Something went wrong. Either you do not have the correct amount of earbuds or I don't have the correct amount of keys.");
                }
            }
            else if (UserKeysAdded % SellPricePerKey != 0 && !IsOverpaying)
            {
                // Count refined and convert to keys -- X scrap per key
                OverpayNumKeys = UserKeysAdded / SellPricePerKey;
                ExcessRefined = UserKeysAdded - (OverpayNumKeys * SellPricePerKey);
                string refined = string.Format("{0}", ExcessRefined);
                Trade.SendMessage("You put up enough keys for " + OverpayNumKeys + " earbud(s), with " + refined + " key(s) extra.");
                Bot.log.Success("User put up enough keys for " + OverpayNumKeys + " earbud(s), with " + refined + " key(s) extra.");
                if (OverpayNumKeys == 0)
                {
                    double AdditionalRefined = (SellPricePerKey) - ExcessRefined;
                    string addRef = string.Format("{0:N2}", AdditionalRefined);
                    errors.Add("ERROR: You need to add " + addRef + " keys for an earbud.");
                    Bot.log.Warn("User doesn't have enough keys added, and needs to add " + addRef + " keys for an earbud.");
                }
                else if (OverpayNumKeys >= 1)
                {
                    errors.Add("You have put up more keys than what I'm asking.");
                    AskOverpay = true;
                }
            }
            else if (UserKeysAdded > 0 && !IsOverpaying)
            {
                if (KeyCount < BotBudsAdded * SellPricePerKey || (KeyCount > BotBudsAdded * SellPricePerKey))
                {
                    errors.Add("You must put up exactly " + String.Format("{0}", (SellPricePerKey)) + " keys per earbud.");
                }
            }

            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }

            return errors.Count == 0;
        }

        public void TradeCountInventory(bool message)
        {
            // Let's count our inventory
            Inventory.Item[] inventory = Trade.MyInventory.Items;
            InventoryKeys = 0;
            InventoryBuds = 0;
            foreach (Inventory.Item item in inventory)
            {
                var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                if (item.Defindex == 5021)
                {
                    InventoryKeys++;
                }
                else if (item.Defindex == 143)
                {
                    InventoryBuds++;
                }
            }
            if (message)
            {
                double MetalToRef = InventoryBuds;
                string refined = string.Format("{0}", MetalToRef);
                Trade.SendMessage("Current stock: I have " + refined + " earbud(s) " + " and " + InventoryKeys + " key(s) in my backpack.");
                Bot.log.Success("Current stock: I have " + refined + " earbud(s) " + " and " + InventoryKeys + " key(s) in my backpack.");
            }
            HasCounted = true;
        }

        public void ReInit()
        {
            UserKeysAdded = 0;
            UserBudsAdded = 0;
            BotBudsAdded = 0;
            BotKeysAdded = 0;
            OverpayNumKeys = 0;
            PreviousKeys = 0;
            ExcessRefined = 0.0;
            WhileLoop = 0;
            InvalidItem = 0;
            HasErrorRun = false;
            ChooseDonate = false;
            AskOverpay = false;
            IsOverpaying = false;
            HasCounted = false;
            currentSID = OtherSID;
        }

        private void OnInviteTimerElapsed(object source, ElapsedEventArgs e, EChatEntryType type)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Hello!  Send me a trade invite to begin trading!");
            Bot.log.Success("Sent welcome message.");
            inviteMsgTimer.Enabled = false;
            inviteMsgTimer.Stop();
        }

        public void ResetTrade(bool message)
        {
            foreach (var item in Trade.MyInventory.Items)
            {
                Trade.RemoveItem(item.Id);
            }
            BotBudsAdded = 0;
            BotKeysAdded = 0;
            ChooseDonate = false;
            TradeCountInventory(message);
            Trade.SendMessage("Something went wrong! Scroll up to read the errors.");
            Bot.log.Warn("Something went wrong! I am resetting the trade.");
            Trade.SendMessage("I have reset the trade. Please try again. (If you chose to donate, you will need to type \"donate\" again)");
            Bot.log.Success("Reset trade.");
        }
    }
}