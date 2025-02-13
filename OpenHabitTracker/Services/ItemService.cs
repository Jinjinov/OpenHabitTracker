using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class ItemService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public ItemModel? SelectedItem { get; set; }

    public ItemModel? NewItem { get; set; }

    public async Task Initialize(ItemsModel? items)
    {
        if (items is not null)
        {
            if (items.Items is null)
            {
                IReadOnlyList<ItemEntity> itms = await _appData.DataAccess.GetItems(items.Id);

                items.Items = itms.Select(i => new ItemModel
                {
                    Id = i.Id,
                    ParentId = i.ParentId,
                    Title = i.Title,
                    DoneAt = i.DoneAt
                }).ToList();
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

        await _appData.DataAccess.AddItem(item);

        NewItem.Id = item.Id;

        NewItem = new();
    }

    public async Task UpdateItem(string title)
    {
        if (SelectedItem is null)
            return;

        SelectedItem.Title = title;

        if (await _appData.DataAccess.GetItem(SelectedItem.Id) is ItemEntity item)
        {
            item.Title = SelectedItem.Title;

            await _appData.DataAccess.UpdateItem(item);
        }
    }

    public async Task SetIsDone(ItemModel item, bool done) // ItemsModel? Items
    {
        DateTime now = DateTime.Now;

        item.DoneAt = done ? now : null;

        if (await _appData.DataAccess.GetItem(item.Id) is ItemEntity itemEntity)
        {
            itemEntity.DoneAt = item.DoneAt;

            await _appData.DataAccess.UpdateItem(itemEntity);
        }

        // TODO:: when all habit items are done, habit is done
        // TODO:: when all task items are done, task is done
    }

    public async Task DeleteItem(ItemsModel? items, ItemModel item)
    {
        items?.Items?.Remove(item);

        await _appData.DataAccess.RemoveItem(item.Id);
    }
}
