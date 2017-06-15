//l.kasprzyk@gmail.com
using System;
using System.Collections.Generic;
using libEziTfidf;

namespace gtktfidf
{	
	public class DocumentTreeNode
	{	
		public static Gtk.TreeStore getDocumentsNodesStore(List<Document> documents)
		{	
			Gtk.TreeStore store = new Gtk.TreeStore (typeof (Document));
			if(documents.Count > 0)
			{
				Gtk.TreeIter iter = Gtk.TreeIter.Zero;
			
				documents.ForEach(doc =>
				{
					if(doc.Documents != null)
						iter = store.AppendValues(doc);
					else
					{
						if(iter.Stamp != Gtk.TreeIter.Zero.Stamp)
							store.AppendValues(iter,doc);
						else
							store.AppendValues(doc);
					}
					
				});
			}			
			return store;			
		}
	}
}

