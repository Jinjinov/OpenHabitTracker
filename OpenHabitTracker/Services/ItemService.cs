using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class ItemService(ClientState clientState) : IItemService
{
    private readonly ClientState _clientState = clientState;

    public ItemModel? SelectedItem { get; set; }

    public ItemModel? NewItem { get; set; }

    public async Task Initialize(ItemsModel? items)
    {
        if (items is not null)
        {
            if (items.Items is null)
            {
                IReadOnlyList<ItemEntity> itms = await _clientState.DataAccess.GetItems(items.Id);

                items.Items = itms.Select(i => i.ToModel()).ToList();

                _clientState.Items ??= new();
                foreach (ItemModel item in items.Items)
                    _clientState.Items[item.Id] = item;
            }
        }

        NewItem ??= new();
    }

    public async Task AddItem(ItemsModel? items)
    {
        if (items is null || NewItem is null)
            return;

        items.Items ??= [];

        items.Items.Add(NewItem);

        NewItem.ParentId = items.Id;

        ItemEntity item = NewItem.ToEntity();

        await _clientState.DataAccess.AddItem(item);

        NewItem.Id = item.Id;

        _clientState.Items ??= new();
        _clientState.Items[NewItem.Id] = NewItem;

        NewItem = new();
    }

    public async Task UpdateItem(string title)
    {
        if (SelectedItem is null)
            return;

        SelectedItem.Title = title;

        if (await _clientState.DataAccess.GetItem(SelectedItem.Id) is ItemEntity item)
        {
            item.Title = SelectedItem.Title;

            await _clientState.DataAccess.UpdateItem(item);
        }
    }

    public async Task SetIsDone(ItemModel item, bool done) // ItemsModel? Items
    {
        DateTime now = DateTime.Now;

        item.DoneAt = done ? now : null;

        if (await _clientState.DataAccess.GetItem(item.Id) is ItemEntity itemEntity)
        {
            itemEntity.DoneAt = item.DoneAt;

            await _clientState.DataAccess.UpdateItem(itemEntity);
        }

        // TODO:: when all habit items are done, habit is done automatically ??? pros & cons ?
        // TODO:: when all task items are done, task is done automatically ??? pros & cons ?
    }

    public async Task DeleteItem(ItemsModel? items, ItemModel item)
    {
        items?.Items?.Remove(item);

        await _clientState.DataAccess.RemoveItem(item.Id);

        _clientState.Items?.Remove(item.Id);
    }
}
