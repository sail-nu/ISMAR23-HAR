using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelController : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text imagePanelText;
    [SerializeField] GameObject imagePanelButtons;
    [SerializeField] Image currentImage;
    private int imageIndex = 0;

    public Sprite[] images { get; set; }

    public void SetActive()
    {
        gameObject.SetActive(true);
    }

    public void SetInactive()
    {
        gameObject.SetActive(false);
    }

    public void OnPhotoPressed()
    {
        if (gameObject.activeInHierarchy)
        {
            SetInactive();
        }
        else
        {
            SetActive();
            imageIndex = 0;
            if (images.Length > 1)
            {
                imagePanelButtons.SetActive(true);
                UpdateImagePanelTitle();
            }
            else
            {
                imagePanelButtons.SetActive(false);
            }
            UpdateImagePanelImage();
        }
    }

    private void UpdateImagePanelTitle()
    {
        imagePanelText.text = "Photo " + (imageIndex + 1).ToString() + "/" + images.Length.ToString(); // Adding 1 to account for zero-indexing
    }

    private void UpdateImagePanelImage()
    {
        currentImage.sprite = images[imageIndex];
    }

    public void OnNextPhotoPressed()
    {
        imageIndex = Mathf.Clamp(imageIndex + 1, 0, images.Length - 1);
        UpdateImagePanelTitle();
        UpdateImagePanelImage();
    }

    public void OnPreviousPhotoPressed()
    {
        imageIndex = Mathf.Clamp(imageIndex - 1, 0, images.Length - 1);
        UpdateImagePanelTitle();
        UpdateImagePanelImage();
    }
}
