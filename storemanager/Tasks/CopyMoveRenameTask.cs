﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    public class CopyMoveTask
        : CancellableTask<TaskResult>
    {
        private IGenericIOManager _source, _target;
        private Uri _sourceUri, _targetUri;
        private CancellableHandler _canceller;

        public CopyMoveTask(IGenericIOManager source, IGenericIOManager target, Uri sourceUri, Uri targetUri, bool forceCopy)
            : base(GetName(source, target, sourceUri, targetUri, forceCopy))
        {
            this._source = source;
            this._target = target;
            this._sourceUri = sourceUri;
            this._targetUri = targetUri;
        }

        public IGenericIOManager Source
        {
            get
            {
                return this._source;
            }
        }

        public IGenericIOManager Target
        {
            get
            {
                return this._target;
            }
        }

        private static String GetName(IGenericIOManager source, IGenericIOManager target, Uri sourceUri, Uri targetUri, bool forceCopy)
        {
            if (ReferenceEquals(source, target) && !forceCopy)
            {
                //Source and Target Store are same so must be a Rename
                return "Move";
            }
            else
            {
                //Different Source and Target store so a Copy/Move
                if (forceCopy)
                {
                    //Source and Target URI are equal so a Copy
                    return "Copy";
                }
                else
                {
                    //Otherwise is a Move
                    return "Move";
                }
            }
        }

        protected override TaskResult RunTaskInternal()
        {
            if (this._target.IsReadOnly) throw new RdfStorageException("Cannot Copy/Move a Graph when the Target is a read-only Store!");

            switch (this.Name)
            {
                case "Move":
                    //Move a Graph 
                    if (ReferenceEquals(this._source, this._target) && this._source is IUpdateableGenericIOManager)
                    {
                        //If the Source and Target are identical and it supports SPARQL Update natively then we'll just issue a MOVE command
                        this.Information = "Issuing a MOVE command to renamed Graph '" + this._sourceUri.ToSafeString() + "' to '" + this._targetUri.ToSafeString() + "'";
                        SparqlParameterizedString update = new SparqlParameterizedString();
                        update.CommandText = "MOVE";
                        if (this._sourceUri == null)
                        {
                            update.CommandText += " DEFAULT TO";
                        }
                        else
                        {
                            update.CommandText += " GRAPH @source TO";
                            update.SetUri("source", this._sourceUri);
                        }
                        if (this._targetUri == null)
                        {
                            update.CommandText += " DEFAULT";
                        }
                        else
                        {
                            update.CommandText += " GRAPH @target";
                            update.SetUri("target", this._targetUri);
                        }
                        ((IUpdateableGenericIOManager)this._source).Update(update.ToString());
                        this.Information = "MOVE command completed OK, Graph renamed to '" + this._targetUri.ToString() + "'";
                    }
                    else
                    {
                        //Otherwise do a load of the source graph writing through to the target graph
                        IRdfHandler handler;
                        IGraph g = null;
                        if (this._target.UpdateSupported)
                        {
                            //If Target supports update then we'll use a WriteToStoreHandler combined with a GraphUriRewriteHandler
                            handler = new WriteToStoreHandler(this._target, this._targetUri);
                            handler = new GraphUriRewriteHandler(handler, this._targetUri);
                        }
                        else
                        {
                            //Otherwise we'll use a GraphHandler and do a save at the end
                            g = new Graph();
                            handler = new GraphHandler(g);
                        }
                        this._canceller = new CancellableHandler(handler);
                        if (this.HasBeenCancelled) this._canceller.Cancel();

                        //Now start reading out the data
                        this.Information = "Copying data from Graph '" + this._sourceUri.ToSafeString() + "' to '" + this._targetUri.ToSafeString() + "'";
                        this._source.LoadGraph(this._canceller, this._sourceUri);

                        //If we weren't moving the data directly need to save the resulting graph now
                        if (g != null)
                        {
                            this.Information = "Saving copied data to Target Store...";
                            this._target.SaveGraph(g);
                        }

                        //And finally since we've done a copy (not a move) so far we need to delete the original graph
                        //to effect a rename
                        if (this._source.DeleteSupported)
                        {
                            this.Information = "Removing source graph to complete the move operation";
                            this._source.DeleteGraph(this._sourceUri);

                            this.Information = "Move completed OK, Graph moved to '" + this._targetUri.ToSafeString() + "'" + (ReferenceEquals(this._source, this._target) ? String.Empty : " on " + this._target.ToString());
                        }
                        else
                        {
                            this.Information = "Copy completed OK, Graph copied to '" + this._targetUri.ToSafeString() + "'" + (ReferenceEquals(this._source, this._target) ? String.Empty : " on " + this._target.ToString()) + ".  Please note that as the Source Triple Store does not support deleting Graphs the Graph remains present in the Source Store";
                        }
                    }

                    break;

                case "Copy":
                    if (ReferenceEquals(this._source, this._target) && this._source is IUpdateableGenericIOManager)
                    {
                        //If the Source and Target are identical and it supports SPARQL Update natively then we'll just issue a COPY command
                        this.Information = "Issuing a COPY command to copy Graph '" + this._sourceUri.ToSafeString() + "' to '" + this._targetUri.ToSafeString() + "'";
                        SparqlParameterizedString update = new SparqlParameterizedString();
                        update.CommandText = "COPY";
                        if (this._sourceUri == null)
                        {
                            update.CommandText += " DEFAULT TO";
                        }
                        else
                        {
                            update.CommandText += " GRAPH @source TO";
                            update.SetUri("source", this._sourceUri);
                        }
                        if (this._targetUri == null)
                        {
                            update.CommandText += " DEFAULT";
                        }
                        else
                        {
                            update.CommandText += " GRAPH @target";
                            update.SetUri("target", this._targetUri);
                        }
                        ((IUpdateableGenericIOManager)this._source).Update(update.ToString());
                        this.Information = "COPY command completed OK, Graph copied to '" + this._targetUri.ToSafeString() + "'";
                    }
                    else
                    {
                        //Otherwise do a load of the source graph writing through to the target graph
                        IRdfHandler handler;
                        IGraph g = null;
                        if (this._target.UpdateSupported)
                        {
                            //If Target supports update then we'll use a WriteToStoreHandler combined with a GraphUriRewriteHandler
                            handler = new WriteToStoreHandler(this._target, this._targetUri);
                            handler = new GraphUriRewriteHandler(handler, this._targetUri);
                        }
                        else
                        {
                            //Otherwise we'll use a GraphHandler and do a save at the end
                            g = new Graph();
                            handler = new GraphHandler(g);
                        }
                        this._canceller = new CancellableHandler(handler);
                        if (this.HasBeenCancelled) this._canceller.Cancel();

                        //Now start reading out the data
                        this.Information = "Copying data from Graph '" + this._sourceUri.ToSafeString() + "' to '" + this._targetUri.ToSafeString() + "'";
                        this._source.LoadGraph(this._canceller, this._sourceUri);

                        //If we weren't moving the data directly need to save the resulting graph now
                        if (g != null)
                        {
                            this.Information = "Saving copied data to Store...";
                            this._target.SaveGraph(g);
                        }

                        this.Information = "Copy completed OK, Graph copied to '" + this._targetUri.ToSafeString() + "'" + (ReferenceEquals(this._source, this._target) ? String.Empty : " on " + this._target.ToString());
                    }

                    break;
            }

            return new TaskResult(true);
        }

        protected override void CancelInternal()
        {
            if (this._canceller != null)
            {
                this._canceller.Cancel();
            }
        }
    }
}
