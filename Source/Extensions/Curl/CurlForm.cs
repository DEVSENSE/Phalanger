using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using PHP.Core;
using System.IO;

namespace PHP.Library.Curl
{

    class CurlForm
    {

        /// <summary>
        /// This represent one item to send with HTTP_POST_FORM
        /// </summary>
        internal class FormDataItem
        {
            private string name;
            private object data;

            public string Name
            {
                get { return name; }
            }

            public object Data
            {
                get { return data; }
            }


            public FormDataItem(string name, object data)
            {
                this.name = name;
                this.data = data;
            }

        }

        internal class FormFileItem : FormDataItem
        {
            private string fileName;
            private string contentType;

            public string FileName
            {
                get { return fileName; }
            }


            public string ContentType
            {
                get { return contentType; }
            }


            public FormFileItem(string name, object data, string fileName, string contentType) :
                base(name, data)
            {
                if (contentType == null)
                    contentType = Utils.ContentTypeForFilename(fileName);

                this.fileName = fileName;
                this.contentType = contentType;
            }

        }


        public static CurlForm Create(PhpArray arr)
        {
            string type = null;
            string filename;

            CurlForm form = new CurlForm();

            //go through items and if item starts with @ we have to treat it as file
            foreach (var item in arr)
            {
                var strValue = PHP.Core.Convert.ObjectToString(item.Value);

                if (strValue[0] == '@')
                {
                    int index = strValue.IndexOf(";type=");
                    if (index != -1)
                        type = strValue.Substring(index + ";type=".Length);


                    index = strValue.IndexOf(";filename=");
                    if (index != -1)
                    {
                        filename = strValue.Substring(index + ";filename=".Length);
                        //filename = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, filename);
                    }
                    else
                    {
                        //filename = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, strValue.Substring(1));
                        filename = strValue.Substring(1);
                    }

                    form.AddFile(item.Key.String,
                        filename,
                        type != null ? type : "application/octet-stream",
                        item.Value
                        );
                    //Code from PHP CURL extension:
                    //error = curl_formadd(&first, &last,
                    //                CURLFORM_COPYNAME, string_key,
                    //                CURLFORM_NAMELENGTH, (long)string_key_len - 1,
                    //                CURLFORM_FILENAME, filename ? filename + sizeof(";filename=") - 1 : postval,
                    //                CURLFORM_CONTENTTYPE, type ? type + sizeof(";type=") - 1 : "application/octet-stream",
                    //                CURLFORM_FILE, postval,
                    //                CURLFORM_END);
                }
                else
                {
                    form.AddData(item.Key.String, item.Value);

                    //Code from PHP CURL extension:
                    //error = curl_formadd(&first, &last,
                    //                             CURLFORM_COPYNAME, string_key,
                    //                             CURLFORM_NAMELENGTH, (long)string_key_len - 1,
                    //                             CURLFORM_COPYCONTENTS, postval,
                    //                             CURLFORM_CONTENTSLENGTH, (long)Z_STRLEN_PP(current),
                    //                             CURLFORM_END);
                }
            }

            return form;
        }

        private CurlForm()
        {
        }


        private LinkedList<FormDataItem> formData = new LinkedList<FormDataItem>();

        public LinkedList<FormDataItem> Data
        {
            get { return formData; }
        }

        internal void AddFile(string key, string filename, string type, object file)
        {
            var item = new FormFileItem(key, file, filename, type);
            formData.AddLast(item);

        }
        internal void AddData(string key, object data)
        {
            var item = new FormDataItem(key, data);
            formData.AddLast(item);
        }

    }
}


//// used by FormAdd for temporary storage
//typedef struct FormInfo {
//  char *name;
//  bool name_alloc;
//  size_t namelength;
//  char *value;
//  bool value_alloc;
//  size_t contentslength;
//  char *contenttype;
//  bool contenttype_alloc;
//  long flags;
//  char *buffer;      /* pointer to existing buffer used for file upload */
//  size_t bufferlength;
//  char *showfilename; /* The file name to show. If not set, the actual
//                         file name will be used */
//  bool showfilename_alloc;
//  char *userp;        /* pointer for the read callback */
//  struct curl_slist* contentheader;
//  struct FormInfo *more;
//} FormInfo;