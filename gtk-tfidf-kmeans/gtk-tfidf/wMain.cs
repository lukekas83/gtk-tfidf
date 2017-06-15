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
	private TFIDFKM tfidf = null;	
	private TextTag tagyellowcolorstemmed,tagyellowcoloorg;
	
	void  InitGui()
	{
		Gtk.TreeViewColumn documentColumn = new Gtk.TreeViewColumn();
		documentColumn.Title = "Dokument"; 
		Gtk.CellRendererText documentCell = new Gtk.CellRendererText ();		
		documentColumn.PackStart (documentCell, true);		
		nvDocuments.AppendColumn (documentColumn);
		documentColumn.AddAttribute (documentCell, "text", 0);
		documentColumn.SetCellDataFunc (documentCell, new Gtk.TreeCellDataFunc (RenderKlassName)); 
		nvDocuments.Model = DocumentTreeNode.getDocumentsNodesStore(new List<Document>());
		//
		tagyellowcolorstemmed = new TextTag("tagyellowcolor");
		tagyellowcoloorg = new TextTag("tagyellowcolor");
		Gdk.Color yellowcolor = new Gdk.Color();
		Gdk.Color.Parse("yellow", ref yellowcolor);
		tagyellowcolorstemmed.BackgroundGdk = yellowcolor;
		tagyellowcoloorg.BackgroundGdk = yellowcolor;
		textOrgDoc.Buffer.TagTable.Add(tagyellowcoloorg);
		textStemmedDoc.Buffer.TagTable.Add(tagyellowcolorstemmed);
	}

	public wMain () : base(Gtk.WindowType.Toplevel)
	{
		Build();
		nvDocuments.NodeSelection.Changed += delegate(object sender, EventArgs e) 
		{
			 displayDoc();
		};
		InitGui();
	}		
	
	private void RenderKlassName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		Document doc = (Document) model.GetValue (iter, 0);
		if(doc.Grupa != null)
			(cell as Gtk.CellRendererText).Text = doc.Grupa;
		else
			(cell as Gtk.CellRendererText).Text = doc.Klasa;
	}

	private void init()
    {
		initTfidf(fcDocumentsDb.Filename, fcKeywords.Filename);
        var db = tfidf.Db;      
		Gtk.Application.Invoke(delegate
		{
			nvDocuments.Model = DocumentTreeNode.getDocumentsNodesStore(db);        	
			btnRank.Sensitive = true;
		});        
	}

    private void initTfidf(string dbfile, string keywordsfile)
    {        
        tfidf = new TFIDFKM(dbfile, keywordsfile);
    }

	private void setChildColorizedText(Document parentdoc, Document childdoc)
	{
		textStemmedDoc.Buffer.Clear();
		var iter = textStemmedDoc.Buffer.GetIterAtOffset(0);		
		string[] childterms = childdoc.CleanedText.Split(" ".ToCharArray());		
		string[] parentterms = parentdoc.CleanedText.Split(" ".ToCharArray());
		Array.ForEach(childterms, term =>
		{
			if(Array.IndexOf(parentterms, term) > -1)
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
	
	private void setParentColorizedText(Document parentdoc, Document childdoc)
	{
		textOrgDoc.Buffer.Clear();
		var iter = textOrgDoc.Buffer.GetIterAtOffset(0);		
		string[] childterms = childdoc.CleanedText.Split(" ".ToCharArray());		
		string[] parentterms = parentdoc.CleanedText.Split(" ".ToCharArray());
		Array.ForEach(parentterms, term =>
		{
			if(Array.IndexOf(childterms, term) > -1)
			{
				textOrgDoc.Buffer.InsertWithTags(ref iter, term,tagyellowcoloorg);
				textOrgDoc.Buffer.Insert(ref iter," ");
			}
			else
			{
				textOrgDoc.Buffer.Insert(ref iter, term);
				textOrgDoc.Buffer.Insert(ref iter," ");
			}
		});
	}
	
    private void displayDoc()
    {
        Document doc = null;    		
		TreeIter iter = new TreeIter();
		TreeSelection selection = nvDocuments.Selection;
		TreeModel model;		
		if(selection.GetSelected(out model, out iter))
		{			
			doc = (Document)model.GetValue(iter,0);			
			lblScore.Text = string.Format("sim(D,C) = {0}", doc.Score);
			lblOrgDocTermsCounted.Text = string.Format("{0} termów", doc.OrgTxtWordCnt.ToString());
			lblStemmedDocTermsCounted.Text = string.Format("{0} termów", doc.CleanedTxtWordCnt.ToString());					
			
			if(doc.Parentdoc != null)
			{
				setChildColorizedText(doc.Parentdoc,doc);
				setParentColorizedText(doc.Parentdoc,doc);
			}
			else
			{
				setChildColorizedText(doc, doc);
				setParentColorizedText(doc ,doc);
				lblScore.Text = string.Format("sim(D,C) = {0}", 1);
			}
		}		
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
		double j = 0;
		int brakpoprawyprzyiteracji = 0;
		nvDocuments.Model = 
			DocumentTreeNode.getDocumentsNodesStore(tfidf.rank((int)sbKm.Value, (int)sbIteracji.Value,
			                       					ref j, ref brakpoprawyprzyiteracji));
		
		lblJValue.Text = string.Concat(j.ToString(), " uzyskany przy ", brakpoprawyprzyiteracji.ToString(), " iteracji");
	}
	
	protected virtual void OnAboutActionActivated (object sender, System.EventArgs e)
	{
		MessageDialog md = new MessageDialog (this, 
		                                      DialogFlags.DestroyWithParent,
		                                      MessageType.Info, 
		                                      ButtonsType.Close, "TFIDF\nk-means");
		md.Run ();
		md.Destroy();
	}
	
	protected virtual void OnQuitActionActivated (object sender, System.EventArgs e)
	{
		Application.Quit();
	}
}