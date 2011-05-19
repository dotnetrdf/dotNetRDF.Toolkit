﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    public class ImportFileTask : BaseImportTask
    {
        private String _file;

        public ImportFileTask(IGenericIOManager manager, String file, Uri targetUri, int batchSize)
            : base("Import File", manager, targetUri, batchSize)
        {
            this._file = file;
        }

        protected override void ImportUsingHandler(IRdfHandler handler)
        {
            this.Information = "Importing from File " + this._file;
            try
            {
                //Assume a RDF Graph
                IRdfReader reader = MimeTypesHelper.GetParser(MimeTypesHelper.GetMimeTypes(Path.GetExtension(this._file)));
                FileLoader.Load(handler, this._file, reader);
            }
            catch (RdfParserSelectionException)
            {
                //Assume a RDF Dataset
                FileLoader.LoadDataset(handler, this._file);
            }
        }
    }

    public class ImportUriTask : BaseImportTask
    {
        private Uri _u;

        public ImportUriTask(IGenericIOManager manager, Uri u, Uri targetUri, int batchSize)
            : base("Import URI", manager, targetUri, batchSize)
        {
            this._u = u;
        }

        protected override void ImportUsingHandler(IRdfHandler handler)
        {
            this.Information = "Importing from URI " + this._u.ToString();
            try
            {
                //Assume a RDF Graph
                UriLoader.Load(handler, this._u);
            }
            catch (RdfParserSelectionException)
            {
                //Assume a RDF Dataset
                UriLoader.LoadDataset(handler, this._u);
            }
        }

        protected override Uri GetDefaultTargetUri()
        {
            return this._u;
        }
    }
}
