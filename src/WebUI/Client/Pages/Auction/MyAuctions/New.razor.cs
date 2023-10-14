﻿using Blazored.Toast.Services;
using Cegeka.Auction.Domain.Enums;
using Cegeka.Auction.WebUI.Shared.Auction;
using Cegeka.Auction.WebUI.Shared.Common;
using Cegeka.Auction.WebUI.Shared.TodoLists;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;


namespace Cegeka.Auction.WebUI.Client.Pages.Auction.MyAuctions;

public partial class New
{
    [Inject]
    public IAuctionsClient AuctionsClient { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Inject]
    public IToastService toastService { get; set; }

    public AuctionItemDetailsVM? Model { get; set; }

    public DeliveryMethod[] Methods = (DeliveryMethod[])Enum.GetValues(typeof(DeliveryMethod));

    public Category[] Categories = (Category[])Enum.GetValues(typeof(Category));

    public List<IBrowserFile> loadedFiles = new();

    public int maxAllowedFiles = 10;

    public bool isLoading;

    public string ValidationMessage { get; set; } = string.Empty;


    public IEnumerable<SelectListItem> availableCurrencies = Enum.GetValues(typeof(Currencies))
       .Cast<Currencies>()
       .Select(p => new SelectListItem
       {
           Value = ((int)p).ToString(),
           Text = p.ToString()
       })
       .ToList();


    protected async Task ShowWarnings(AuctionItemDTO item, string auctionType)
    {
        string message;

        if (auctionType == "add")
        {
            message = "The auction has been successfully added!";
            toastService.ShowSuccess(message);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Model = new AuctionItemDetailsVM();
        Model.Auction.StartDate = DateTime.Now;
        Model.Auction.EndDate = DateTime.Now;
    }
    private async Task LoadFiles(InputFileChangeEventArgs e)
    {
        isLoading = false;
        ValidationMessage = string.Empty;

        foreach (var file in e.GetMultipleFiles(maxAllowedFiles))
        {
            isLoading = true;

            if (!Regex.IsMatch(file.ContentType, @"^image\/(jpeg|png)$"))
            {
                // Invalid file type
                ValidationMessage = $"File {file.Name} is not of an allowed type.";
                return;
            }


            if (file.Size > 5 * 1024 * 1024) // 5 MB
            {
                ValidationMessage = $"File {file.Name} exceeds the maximum size of 5 MB.";
                return;
            }

            if (Model.Auction.Images.Count >= maxAllowedFiles)
            {
                ValidationMessage = $"You can upload a maximum of {maxAllowedFiles} images.";
                return;
            }

            try
            {
                // add image to auction item
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var imgSrc = $"data:{file.ContentType};base64,{base64}";
                Model.Auction.Images.Add(imgSrc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        isLoading = false;
    }

    public async Task AddAuction()
    {
        if (loadedFiles.Any())
        {
            foreach (var file in loadedFiles)
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    await file.OpenReadStream().CopyToAsync(memoryStream);
                    var bytes = memoryStream.ToArray();
                    var base64 = Convert.ToBase64String(bytes);
                    var imgSrc = $"data:{file.ContentType};base64,{base64}";
                    Model.Auction.Images.Add(imgSrc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        if (Model.Auction.Images != null && Model.Auction.Images.Any())
        {
            await AuctionsClient.AddAuctionAsync(Model.Auction);
            await ShowWarnings(Model.Auction, "add");
            Navigation.NavigateTo("/auctions");
        }
        else
        {
            ValidationMessage = "At least one image is required to create the auction.";
        }


    }

    public async Task RemoveImage(int index)
    {
        if (index >= 0 && index < Model.Auction.Images.Count)
        {
            Model.Auction.Images.RemoveAt(index);
            StateHasChanged();
        }
    }

    private RenderFragment RenderImages()
    {
        return builder =>
        {
            for (int i = 0; i < Model.Auction.Images.Count; i++)
            {
                var index = i;
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "display: inline-block; position: relative; margin-right: 10px;");

                builder.OpenElement(2, "img");
                builder.AddAttribute(3, "src", Model.Auction.Images[index]);
                builder.AddAttribute(4, "style", "max-height: 100px;");
                builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, async e =>
                {
                    await RemoveImage(index);
                }));
                builder.CloseElement();

                builder.OpenElement(6, "span");
                builder.AddAttribute(7, "style", "position: absolute; top: -5px; right: -5px; display: inline-block; width: 20px; height: 20px; text-align: center; background-color: #f44336; color: white; cursor: pointer; border-radius: 50%;");
                builder.AddAttribute(8, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, async e =>
                {
                    await RemoveImage(index);
                }));
                builder.AddContent(9, "X");
                builder.CloseElement();

                builder.CloseElement();
            }
        };
    }
}
