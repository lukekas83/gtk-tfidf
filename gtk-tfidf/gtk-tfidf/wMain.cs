//l.kasprzyk@gmail.com
using System;
using System.Linq;
using Gtk;
using libEziTfidf;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using gtktfidf;

public partial class wMain : Gtk.Window
{
	private TFIDF tfidf = null;
	private TextTag tagyellowcolororg,tagyellowcolorstemmed;
	public wMain () : base(Gtk.WindowType.Toplevel)
	{
		Build();
		nvDocuments.NodeSelection.Changed += delegate(object sender, EventArgs e) 
		{
			 displayDoc();
		};
		nvDocuments.NodeStore = DocumentTreeNode.getDocumentsNodesStore(new List<Document>());	
		nvDocuments.AppendColumn ("Nagłówek dokumentu", new Gtk.CellRendererText (), "text", 0);	
		
		tagyellowcolororg = new TextTag("tagyellowcolor");
		tagyellowcolorstemmed = new TextTag("tagyellowcolor");
		Gdk.Color yellowcolor = new Gdk.Color();
		Gdk.Color.Parse("yellow", ref yellowcolor);
		tagyellowcolororg.BackgroundGdk = yellowcolor;
		tagyellowcolorstemmed.BackgroundGdk = yellowcolor;
		textOrgDoc.Buffer.TagTable.Add(tagyellowcolororg);
		textStemmedDoc.Buffer.TagTable.Add(tagyellowcolorstemmed);		
	}		
	
	private TreeModel CreateCompletionModel()
	{
		ListStore store = new ListStore (typeof (string));
		string[] words = Document.KeywordsOrg;
		Array.ForEach(words, w => store.AppendValues(w));		
        return store;
	}
	
	private void init()
    {
		initTfidf(fcDocumentsDb.Filename, fcKeywords.Filename);
        var db = tfidf.Db;      
		Gtk.Application.Invoke(delegate
		{
			nvDocuments.NodeStore = DocumentTreeNode.getDocumentsNodesStore(db);
			var e = nvDocuments.NodeStore.GetEnumerator();
			if(e.MoveNext())
				nvDocuments.NodeSelection.SelectNode((ITreeNode)e.Current);
			entryQuery.Completion = new EntryCompletion ();
        	entryQuery.Completion.Model = CreateCompletionModel();
        	entryQuery.Completion.TextColumn = 0;
			btnRank.Sensitive = true;
		});        
	}

    private void initTfidf(string dbfile, string keywordsfile)
    {        
        tfidf = new TFIDF(dbfile, keywordsfile);
    }

    private void displayDoc()
    {
        Document doc = null;    
		Gtk.NodeSelection selection = nvDocuments.NodeSelection;
        DocumentTreeNode node = (DocumentTreeNode) selection.SelectedNode;
		if(node != null)
		{
			doc = node.Document;
			lblHeader.Text = doc.Header;
			lblScore.Text = string.Format("sim(Q,D) = {0}", doc.Score);
			lblOrgDocTermsCounted.Text = string.Format("{0} termów", doc.OrgTxtWordCnt.ToString());
			lblStemmedDocTermsCounted.Text = string.Format("{0} termów", doc.CleanedTxtWordCnt.ToString());			
			setOrgColorizedText(doc.Text);
			setStemmedColorizedText(doc.CleanedText);
		}
    }
	
	private void setOrgColorizedText(string text)
	{
		textOrgDoc.Buffer.Clear();
		var iter = textOrgDoc.Buffer.GetIterAtOffset(0);
		string[] terms = text.Split(" ".ToCharArray());
		int wc = 0;
		List<string> queryterms = new List<string>(Document.cleanDoc(entryQuery.Text, out wc, " ").Split(" ".ToCharArray()));
		Array.ForEach(terms, term =>
		{			
			if((from qt in queryterms where term.ToLower().Contains(qt) && qt != "" select qt).Count() > 0)
			{
				textOrgDoc.Buffer.InsertWithTags(ref iter, term,tagyellowcolororg);
				textOrgDoc.Buffer.Insert(ref iter," ");
			}
			else
			{
				textOrgDoc.Buffer.Insert(ref iter, term);
				textOrgDoc.Buffer.Insert(ref iter," ");
			}
		});
	}
	
	private void setStemmedColorizedText(string text)
	{
		textStemmedDoc.Buffer.Clear();
		var iter = textStemmedDoc.Buffer.GetIterAtOffset(0);		
		string[] terms = text.Split(" ".ToCharArray());
		int wc = 0;
		string[] queryterms = Document.cleanDoc(entryQuery.Text, out wc, " ").Split(" ".ToCharArray());
		Array.ForEach(terms, term =>
		{
			if(Array.IndexOf(queryterms, term) > -1)
			{
				textStemmedDoc.Buffer.InsertWithTags(ref iter, term,tagyellowcolorstemmed);
				textStemmedDoc.Buffer.Insert(ref iter," ");
			}
			else
			{
				textStemmedDoc.Buffer.Insert(ref iter, term);
				textStemmedDoc.Buffer.Insert(ref iter," ");
			}
		});
	}		
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
	protected virtual void OnFcDocumentsDbSelectionChanged (object sender, System.EventArgs e)
	{
		if(!string.IsNullOrEmpty(fcKeywords.Filename))
			doInitJob();
	}	
	
	private void doInitJob()		
	{
		Thread t = new Thread(new ThreadStart(init));
		t.Start();		
	}
	
	protected virtual void OnFcKeywordsSelectionChanged (object sender, System.EventArgs e)
	{
		if(!string.IsNullOrEmpty(fcDocumentsDb.Filename))
			doInitJob();
	}
	protected virtual void OnBtnRankClicked (object sender, System.EventArgs e)
	{
		if(!string.IsNullOrEmpty(entryQuery.Text))
		{
			nvDocuments.NodeStore = DocumentTreeNode.getDocumentsNodesStore(tfidf.search(entryQuery.Text,tfidf.Db.Count));
			var en = nvDocuments.NodeStore.GetEnumerator();
			if(en.MoveNext())
				nvDocuments.NodeSelection.SelectNode((ITreeNode)en.Current);
		}
	}	
	protected virtual void OnAboutActionActivated (object sender, System.EventArgs e)
	{
		MessageDialog md = new MessageDialog (this, 
		                                      DialogFlags.DestroyWithParent,
		                                      MessageType.Info, 
		                                      ButtonsType.Close, "Wyszukiwarka TFIDF\n");
		md.Run ();
		md.Destroy();
	}
	
	protected virtual void OnQuitActionActivated (object sender, System.EventArgs e)
	{
		Application.Quit();
	}
}

