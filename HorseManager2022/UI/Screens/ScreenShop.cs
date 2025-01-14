﻿using HorseManager2022.Enums;
using HorseManager2022.Interfaces;
using HorseManager2022.Models;
using HorseManager2022.UI.Components;
using HorseManager2022.UI.Dialogs;
using System;
using System.Reflection.Emit;

namespace HorseManager2022.UI.Screens
{
    internal class ScreenShop<T, U> : ScreenTable<T, U> where T : IExchangeable
    {
        // Constants
        private const int DIALOG_POS_X = 20;
        private const int DIALOG_POS_Y = 15;
        

        // Constructor
        public ScreenShop(Topbar topbar, string title, Screen? previousScreen = null, string[]? propertiesToExclude = null, bool isSelectable = false)
            : base(topbar, title, previousScreen, propertiesToExclude, isSelectable)
        {
        }


        override protected Action GetOptionOnEnter(T item, GameManager? gameManager)
        {
            return () => {

                // Get dialog data
                Event? todayEvent = Event.GetTodayEvent(gameManager);
                string itemType = item.GetType().Name.ToLower();
                string action = (typeof(U) == typeof(Shop)) ? "buy" : "sell";
                int price = item.price;

                // Get Discounted or Increased price
                if (typeof(U) == typeof(Shop) && todayEvent != null && todayEvent.type == EventType.Holiday)
                    price = Utils.GetDiscountedPrice(price);
                else if (todayEvent != null && todayEvent.type == EventType.Holiday)
                    price = Utils.GetIncreasedPrice(price);

                // Build Dialog
                DialogConfirmation dialogConfirmation = new(
                    x: DIALOG_POS_X, y: DIALOG_POS_Y,
                    title: $"{action} {itemType}",
                    message: $"Are you sure you want to {action} {item.name} for {price:C} ?",
                    dialogType: DialogType.Question,
                    previousScreen: this,
                    onConfirm: () => {
                        
                        // Get dialog data
                        bool response = gameManager?.Exchange<T, U>(item) ?? false;
                        string message = response ? $"{item.name} was successfully {action}ed!" : $"You don't have enough money to {action} {item.name}!";
                        DialogType dialogType = response ? DialogType.Success : DialogType.Error;

                        // Build Dialog
                        DialogMessage dialogWarning = new(
                            x: DIALOG_POS_X, y: DIALOG_POS_Y,
                            message: message,
                            dialogType: dialogType,
                            previousScreen: this
                        );
                        dialogWarning.Show();

                    }, onCancel: () => {

                    });

                dialogConfirmation.Show();
            };
        }


    }
}
