using UnityEngine;

namespace Paroxe.PdfRenderer.Examples
{
    public class PDFDocumentRenderToTextureExample : MonoBehaviour
    {
        public int m_Page = 0;
        public PDFAsset document;

        public Texture2D tex;
        PDFDocument pdfDocument;
        PDFRenderer pdfRenderer;
        int pageCount;

#if !UNITY_WEBGL

        void Start()
        {
            pdfDocument = new PDFDocument(document.m_FileContent);//    PDFBytesSupplierExample.PDFSampleByteArray, "");

            if (pdfDocument.IsValid)
            {
                pageCount = pdfDocument.GetPageCount();
                Debug.LogError("PageCount:" + pageCount);

                pdfRenderer = new PDFRenderer();

                tex = pdfRenderer.RenderPageToTexture(pdfDocument.GetPage(m_Page % pageCount), 1024, 1350);
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 8;
                GetComponent<MeshRenderer>().material.mainTexture = tex;
            }
        }

        public void PageUp()
        {
            m_Page--;
            if (m_Page < 0) m_Page = 0;
            pdfRenderer.RenderPageToExistingTexture(pdfDocument.GetPage(m_Page), tex);
        }

        public void PageDown()
        {
            m_Page++;
            if (m_Page >= pageCount) m_Page = pageCount - 1;
            pdfRenderer.RenderPageToExistingTexture(pdfDocument.GetPage(m_Page), tex);
        }

        public void GoToPage(int i)
        {
            if (i >= pageCount || i < 0) return;
            m_Page = i;
            pdfRenderer.RenderPageToExistingTexture(pdfDocument.GetPage(m_Page), tex);
        }
#endif
    }
}
