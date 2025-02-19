﻿using HorseManager2022.Attributes;
using HorseManager2022.Enums;
using HorseManager2022.Interfaces;
using HorseManager2022.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace HorseManager2022.UI.Components
{
    internal class Table<T, U>
    {
        // Constants
        private const int DEFAULT_TABLE_WIDTH = 72;

        // Properties
        public int selectedPosition;
        private readonly bool isSelectable;
        private readonly string[] propertiesToExclude;
        private readonly string title;
        private readonly bool isAddable;

        // Constructor
        public Table(string title, string[]? propertiesToExclude = null, bool isSelectable = false, bool isAddable = false)
        {
            this.title = title;
            this.propertiesToExclude = propertiesToExclude ?? Array.Empty<string>();
            this.isSelectable = isSelectable;
            this.isAddable = isAddable;
        }


        // Methods
        private bool IsRowSelected(int rowIndex) => (rowIndex == selectedPosition) && isSelectable;
        

        // Get Table Items Ordered By Rarity if they have
        public List<T> GetTableItems(GameManager? gameManager)
        {
            List<T>? list = gameManager?.GetList<T, U>().OrderByDescending(x => {
                var rarity = x as IExchangeable;
                return (int)(rarity?.rarity ?? 0);
            }).ToList();

            return list ?? new();
        }
        
        
        public void Show(GameManager? gameManager)
        {
            // Initial verifications
            if (gameManager == null)
                return;

            // Get data
            List<T> items = GetTableItems(gameManager);
            List<string> headers = GetTableHeaders();
            if (items == null || items.Count == 0) {
                headers.Clear();
                headers.Add(Utils.AlignCenter("Nothing to show.", DEFAULT_TABLE_WIDTH));
            }
            int tableWidth = GetTableWidth(headers);

            // Show data
            Console.ResetColor();
            Console.WriteLine();

            DrawLine(tableWidth);

            // Title
            DrawTitle(tableWidth - 2);

            DrawLine(tableWidth);

            // Header
            DrawHeader(headers);

            DrawLine(tableWidth);

            // Content
            DrawContent(items, headers, gameManager);

            if (items.Count != 0 && !isAddable)
                DrawLine(tableWidth);

            // Footer
            if (isAddable) 
            {
                if (items.Count != 0)
                    DrawLine(tableWidth);
                DrawFooter(items, tableWidth);
                DrawLine(tableWidth);
            }

        }


        private List<string> GetTableHeaders()
        {
            List<string> headers = new();

            // Add selection header
            if (isSelectable)
                headers.Add("     ");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            foreach (PropertyDescriptor property in properties)
            {
                if (propertiesToExclude.Contains(property.Name))
                    continue;

                PaddingAttribute? padding = property.Attributes.OfType<PaddingAttribute>().FirstOrDefault();

                int value = padding?.value ?? 0;
                string name = property.DisplayName;
                name = value != 0 ? Utils.AlignCenter($" {name} ", value) : $" {name} ";
                headers.Add(name);
            }

            return headers;
        }


        static private int GetTableWidth(List<string> headers)
        {
            int tableWidth = 0;
            foreach (string header in headers)
                tableWidth += header.Length;

            // Add bar "|" count for each
            tableWidth += headers.Count - 1;

            return tableWidth;
        }


        private void DrawHeader(List<string> headers)
        {
            bool haveItems = headers.Find(x => x.Contains("Nothing to show.")) == null;
            if (!haveItems)
                DrawGapRow(headers);

            foreach (string header in headers)
                Console.Write("|" + header);
            Console.WriteLine("|");

            if (!haveItems)
                DrawGapRow(headers);
        }


        private void DrawContent(List<T> items, List<string> headers, GameManager? gameManager)
        {
            for (int i = 0; i < items.Count; i++)
            {
                DrawRow(items[i], headers, i, gameManager);
                
                if (i < items.Count - 1)
                    DrawGapRow(headers);
            }
        }


        private void DrawFooter(List<T> items, int width)
        {
            Type[] typeArguments = items.GetType().GetGenericArguments();
            string typeName = typeArguments[0].Name;

            if (selectedPosition == items.Count)
                Console.Write("| [X] |");
            else
                Console.Write("| [ ] |");
            Console.Write(Utils.AlignLeft($" Add new {typeName} ", width-6));
            Console.WriteLine("|");
        }


        private void DrawRow(T item, List<string> headers, int rowIndex, GameManager? gameManager)
        {
            // Add selection column
            if (IsRowSelected(rowIndex))
                Console.Write("| [X] ");
            else if (isSelectable)
                Console.Write("| [ ] ");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            foreach (PropertyDescriptor property in properties) 
                DrawColumn(item, property, headers, gameManager);

            Console.WriteLine("|");
        }


        private void DrawGapRow(List<string> headers)
        {
            foreach (string header in headers)
                Console.Write("|" + Utils.AlignCenter("", header.Length));
            Console.WriteLine("|");
        }


        private void DrawColumn(T item, PropertyDescriptor property, List<string> headers, GameManager? gameManager)
        {
            if (propertiesToExclude.Contains(property.Name))
                return;

            string? header = headers.FirstOrDefault(h => h.Contains(property.DisplayName));
            int padding = header?.Length ?? 0;
            string? propertyValue = property.GetValue(item)?.ToString();
            string label = propertyValue ?? "";

            if (propertyValue == null)
                return;

            bool isPercentage = property.Attributes.OfType<IsPercentageAttribute>().FirstOrDefault() != null;
            bool isPrice = property.Attributes.OfType<IsPriceAttribute>().FirstOrDefault() != null;
            IsRarityAttribute? rarityAttribute = property.Attributes.OfType<IsRarityAttribute>().FirstOrDefault();
            IsEnergyAttribute? energyAttribute = property.Attributes.OfType<IsEnergyAttribute>().FirstOrDefault();
            ColorAttribute? colorAttribute = property.Attributes.OfType<ColorAttribute>().FirstOrDefault();
            ConsoleColor color = colorAttribute?.color ?? ConsoleColor.Gray;

            if (rarityAttribute != null)
            {
                Rarity rarity = (Rarity)Enum.Parse(typeof(Rarity), label);
                color = rarityAttribute.GetColor(rarity);
            }
            else if (energyAttribute != null)
            {
                color = energyAttribute.GetColor(int.Parse(label));
            }

            if (isPercentage)
                label += "%";
            else if (isPrice) 
            {
                // Check if it's holiday to show price with 25% discount
                Event? @event = Event.GetTodayEvent(gameManager);
                if (@event != null && @event.type == EventType.Holiday)
                {
                    if (typeof(U) == typeof(Shop))
                        label = Utils.GetDiscountedPrice(int.Parse(label)).ToString();
                    else
                        label = Utils.GetIncreasedPrice(int.Parse(label)).ToString();

                    color = ConsoleColor.DarkGreen;
                }

                label += ",00 €";
            }

            string valueString = Utils.AlignCenter($" {label} ", padding);
            Console.Write("|");
            Console.ForegroundColor = color;
            Console.Write(valueString);
            Console.ResetColor();
        }


        private void DrawLine(int width) => Console.WriteLine("+" + new string('-', width) + "+");


        private void DrawTitle(int width) => Console.WriteLine("| " + Utils.AlignCenter(title, width) + " |");




        /*
        public void DrawTableHorses(List<Horse> horses)
        {
        */

        /*
         * 

        for (int i = 0; i < items.Count; i++)
        {
            T horse = items[i];

            string name = Utils.PadCenter(horse.name, 17);
            string rarity = Utils.PadCenter(horse.rarity.ToString(), 10);
            string energy = Utils.PadCenter(horse.energy.ToString() + "%", 10);
            string resistance = Utils.PadCenter(horse.resistance.ToString(), 14);
            string speed = Utils.PadCenter(horse.speed.ToString(), 9);
            string age = Utils.PadCenter(horse.age.ToString(), 7);

            Console.Write($"|{name}|");

            Console.ForegroundColor = horse.RarityColor();
            Console.Write($"{rarity}");
            Console.ResetColor();
            Console.Write("|");

            Console.ForegroundColor = horse.GetEnergyColor();
            Console.Write($"{energy}");
            Console.ResetColor();
            Console.Write("|");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{resistance}");
            Console.ResetColor();
            Console.Write("|");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{speed}");
            Console.ResetColor();

            Console.WriteLine($"|{age}|");

            // gap between horses
            if (i < horses.Count - 1)
                Console.WriteLine("|                 |          |          |              |         |       |");
        }

        Console.WriteLine("+------------------------------------------------------------------------+");
        Console.WriteLine("Quantity: [" + horses.Count + "]");*/

        /*
        Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("+------------------------------------------------------------------------+");
            Console.WriteLine("|                              Player Horses                             |");
            Console.WriteLine("+------------------------------------------------------------------------+");
            Console.WriteLine("|       Name      |  Rarity  |  Energy  |  Resistance  |  Speed  |  Age  |");
            Console.WriteLine("+------------------------------------------------------------------------+");

            for (int i = 0; i < horses.Count; i++)
            {
                Horse horse = horses[i];

                string name = Utils.PadCenter(horse.name, 17);
                string rarity = Utils.PadCenter(horse.rarity.ToString(), 10);
                string energy = Utils.PadCenter(horse.energy.ToString() + "%", 10);
                string resistance = Utils.PadCenter(horse.resistance.ToString(), 14);
                string speed = Utils.PadCenter(horse.speed.ToString(), 9);
                string age = Utils.PadCenter(horse.age.ToString(), 7);

                Console.Write($"|{name}|");

                Console.ForegroundColor = horse.RarityColor();
                Console.Write($"{rarity}");
                Console.ResetColor();
                Console.Write("|");

                Console.ForegroundColor = horse.GetEnergyColor();
                Console.Write($"{energy}");
                Console.ResetColor();
                Console.Write("|");

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{resistance}");
                Console.ResetColor();
                Console.Write("|");

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{speed}");
                Console.ResetColor();

                Console.WriteLine($"|{age}|");

                // gap between horses
                if (i < horses.Count - 1)
                    Console.WriteLine("|                 |          |          |              |         |       |");
            }

            Console.WriteLine("+------------------------------------------------------------------------+");
            Console.WriteLine("Quantity: [" + horses.Count + "]");
        }

        }*/
    }
}
