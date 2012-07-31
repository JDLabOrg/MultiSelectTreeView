﻿namespace System.Windows.Controls
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Input;

	/// <summary>
	/// Logic for the multiple selection
	/// </summary>
	public class SelectionMultiple : ISelectionStrategy
	{
		public event EventHandler<PreviewSelectionChangedEventArgs> PreviewSelectionChanged;

		private readonly TreeViewEx treeViewEx;

		private BorderSelectionLogic borderSelectionLogic;

		private object lastShiftRoot;

		private TreeViewExItem selectedPreviewItem;

		public SelectionMultiple(TreeViewEx treeViewEx)
		{
			this.treeViewEx = treeViewEx;
		}

		#region Properties

		private static bool IsControlKeyDown
		{
			get
			{
				return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
			}
		}

		private static bool IsShiftKeyDown
		{
			get
			{
				return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
			}
		}

		private TreeViewExItem SelectedPreviewItem
		{
			get
			{
				return selectedPreviewItem;
			}
			set
			{
				if (selectedPreviewItem != null)
				{
					selectedPreviewItem.IsSelectedPreview = false;
				}

				selectedPreviewItem = value;
				if (selectedPreviewItem != null)
				{
					selectedPreviewItem.IsSelectedPreview = true;
				}
			}
		}

		#endregion

		public void ApplyTemplate()
		{
			borderSelectionLogic = new BorderSelectionLogic(
			   treeViewEx,
			   treeViewEx.Template.FindName("selectionBorder", treeViewEx) as Border,
			   TreeViewEx.RecursiveTreeViewItemEnumerable(treeViewEx, false, false));
		}

		public bool Select(TreeViewExItem item)
		{
			if (IsControlKeyDown)
			{
				if (treeViewEx.SelectedItems.Contains(item.DataContext))
				{
					if (!UnSelect(item))
					{
						FocusHelper.Focus(item);
						return false;
					}

					if (item.DataContext == lastShiftRoot)
					{
						lastShiftRoot = null;
					}
				}
				else
				{
					var e = new PreviewSelectionChangedEventArgs(true, item.DataContext);
					OnPreviewSelectionChanged(e);
					if (e.Cancel)
					{
						FocusHelper.Focus(item);
						return false;
					}

					item.IsSelected = true;
					if (!treeViewEx.SelectedItems.Contains(item.DataContext))
					{
						treeViewEx.SelectedItems.Add(item.DataContext);
					}
					lastShiftRoot = item.DataContext;
				}
				FocusHelper.Focus(item);
				SelectedPreviewItem = null;
				return true;
			}
			else
			{
				return SelectCore(item);
			}
		}

		internal bool SelectByRectangle(TreeViewExItem item)
		{
			var e = new PreviewSelectionChangedEventArgs(true, item.DataContext);
			OnPreviewSelectionChanged(e);
			if (e.Cancel)
			{
				FocusHelper.Focus(item);
				lastShiftRoot = item.DataContext;
				return false;
			}

			if (!treeViewEx.SelectedItems.Contains(item.DataContext))
			{
				treeViewEx.SelectedItems.Add(item.DataContext);
			}
			FocusHelper.Focus(item);
			SelectedPreviewItem = null;
			lastShiftRoot = item.DataContext;
			return true;
		}

		public bool SelectCore(TreeViewExItem item)
		{
			if (IsControlKeyDown)
			{
				SelectedPreviewItem = item;
				FocusHelper.Focus(item);
			}
			else if (IsShiftKeyDown && treeViewEx.SelectedItems.Count > 0)
			{
				object firstSelectedItem = lastShiftRoot ?? treeViewEx.SelectedItems.First();
				TreeViewExItem shiftRootItem = treeViewEx.GetTreeViewItemsFor(new List<object> { firstSelectedItem }).First();

				var newSelection = treeViewEx.GetNodesToSelectBetween(shiftRootItem, item).Select(n => n.DataContext).ToList();
				var selectedItems = treeViewEx.SelectedItems.ToList();
				// Remove all items no longer selected
				foreach (var selItem in selectedItems.Where(i => !newSelection.Contains(i)))
				{
					var e = new PreviewSelectionChangedEventArgs(false, selItem);
					OnPreviewSelectionChanged(e);
					if (e.Cancel)
					{
						FocusHelper.Focus(item);
						return false;
					}

					treeViewEx.SelectedItems.Remove(selItem);
				}
				// Add new selected items
				foreach (var newItem in newSelection.Where(i => !selectedItems.Contains(i)))
				{
					var e = new PreviewSelectionChangedEventArgs(true, newItem);
					OnPreviewSelectionChanged(e);
					if (e.Cancel)
					{
						FocusHelper.Focus(item);
						return false;
					}

					treeViewEx.SelectedItems.Add(newItem);
				}
				
				SelectedPreviewItem = null;
				FocusHelper.Focus(item);
			}
			else
			{
				if (treeViewEx.SelectedItems.Count > 0)
				{
					foreach (var selItem in treeViewEx.SelectedItems)
					{
						var e2 = new PreviewSelectionChangedEventArgs(false, selItem);
						OnPreviewSelectionChanged(e2);
						if (e2.Cancel)
						{
							FocusHelper.Focus(item);
							lastShiftRoot = item.DataContext;
							return false;
						}
					}
					
					treeViewEx.SelectedItems.Clear();
				}
				
				var e = new PreviewSelectionChangedEventArgs(true, item.DataContext);
				OnPreviewSelectionChanged(e);
				if (e.Cancel)
				{
					FocusHelper.Focus(item);
					lastShiftRoot = item.DataContext;
					return false;
				}

				treeViewEx.SelectedItems.Add(item.DataContext);
				SelectedPreviewItem = null;
				FocusHelper.Focus(item);
				lastShiftRoot = item.DataContext;
			}

			LastSelectedItem = item;
			return true;
		}

		public bool SelectCurrentBySpace()
		{
			if (SelectedPreviewItem != null)
			{
				// There is a selection preview item that was chosen by Ctrl+Arrow key
				var item = SelectedPreviewItem;
				if (treeViewEx.SelectedItems.Contains(item.DataContext))
				{
					// With Ctrl key, toggle this item selection.
					// Without Ctrl key, always select it.
					if (IsControlKeyDown)
					{
						if (!UnSelect(item)) return false;
						item.IsSelected = false;
					}
				}
				else
				{
					var e = new PreviewSelectionChangedEventArgs(true, item.DataContext);
					OnPreviewSelectionChanged(e);
					if (e.Cancel)
					{
						FocusHelper.Focus(item);
						return false;
					}

					item.IsSelected = true;
					if (!treeViewEx.SelectedItems.Contains(item.DataContext))
					{
						treeViewEx.SelectedItems.Add(item.DataContext);
					}
				}
				FocusHelper.Focus(item);
				return true;
			}
			else
			{
				// An item was chosen without the Ctrl key
				return Select(treeViewEx.GetFocusedItem(TreeViewEx.RecursiveTreeViewItemEnumerable(treeViewEx, false)));
			}
		}

		public bool SelectNextFromKey()
		{
			List<TreeViewExItem> items = TreeViewEx.RecursiveTreeViewItemEnumerable(treeViewEx, false, false).ToList();
			TreeViewExItem item;
			if (IsControlKeyDown && SelectedPreviewItem != null)
			{
				item = treeViewEx.GetNextItem(SelectedPreviewItem, items);
			}
			else
			{
				item = treeViewEx.GetNextItem(treeViewEx.GetFocusedItem(items), items);
			}

			if (item == null)
			{
				return false;
			}

			return SelectCore(item);
		}

		public bool SelectPreviousFromKey()
		{
			List<TreeViewExItem> items = TreeViewEx.RecursiveTreeViewItemEnumerable(treeViewEx, false, false).ToList();
			TreeViewExItem item;
			if (IsControlKeyDown && SelectedPreviewItem != null)
			{
				item = treeViewEx.GetPreviousItem(SelectedPreviewItem, items);
			}
			else
			{
				item = treeViewEx.GetPreviousItem(treeViewEx.GetFocusedItem(items), items);
			}

			if (item == null)
			{
				return false;
			}

			return SelectCore(item);
		}

		public bool UnSelect(TreeViewExItem item)
		{
			var e = new PreviewSelectionChangedEventArgs(false, item.DataContext);
			OnPreviewSelectionChanged(e);
			if (e.Cancel) return false;

			treeViewEx.SelectedItems.Remove(item.DataContext);
			return true;
		}

		public void Dispose()
		{
			if (borderSelectionLogic != null)
			{
				borderSelectionLogic.Dispose();
				borderSelectionLogic = null;
			}

			GC.SuppressFinalize(this);
		}

		protected void OnPreviewSelectionChanged(PreviewSelectionChangedEventArgs e)
		{
			var handler = PreviewSelectionChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		#region ISelectionStrategy Members


		public TreeViewExItem LastSelectedItem
		{
			get;
			private set;
		}

		public void OnLostFocus()
		{
			SelectedPreviewItem = null;
		}

		#endregion
	}
}
