//l.kasprzyk@gmail.com
using System;
using System.Collections.Generic;
using libEziTfidf;

namespace gtktfidf
{
	[Gtk.TreeNode (ListOnly=true)]
	public class DocumentTreeNode : Gtk.TreeNode
	{
		[Gtk.TreeNodeValue (Column=0)]
        public string DocumentHeader
		{
			get
			{
				return document.Header;
			}
		}
		
		private Document document;
		public Document Document 
		{
			get 
			{
				return this.document;
			}
		}

		public DocumentTreeNode (Document document)
		{
			this.document = document;
		}
		
		public static Gtk.NodeStore getDocumentsNodesStore(List<Document> documents)
		{
			Gtk.NodeStore store = new Gtk.NodeStore (typeof (DocumentTreeNode));
			
			documents.ForEach(doc =>
			{
				store.AddNode (new DocumentTreeNode(doc));								
			});
			
			return store;			
		}
	}
}

