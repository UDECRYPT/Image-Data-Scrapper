
async function scrapeImageMetadata(imgUrl) {
    var formData = new FormData();

    // Convert the image URL to Blob and append it to FormData
    const response = await fetch(imgUrl);
    const blob = await response.blob();
    formData.append('imageFile', blob, 'image.jpg');

    var xhr = new XMLHttpRequest();
    xhr.open('POST', 'https://localhost:7090/api/ImageMetadata/GetMetadata', true);
    xhr.onload = async function () {
        if (xhr.status === 200) {
            var response = JSON.parse(xhr.responseText);
            console.log(response);
            if (response.error) {
                alert('Error fetching metadata: ' + response.error);
            } else {
                let metadataDiv = document.getElementById('metadataPopup');
                if (!metadataDiv) {
                    metadataDiv = document.createElement('div');
                    metadataDiv.id = 'metadataPopup';
                    metadataDiv.style.position = 'fixed';
                    metadataDiv.style.top = '10px';
                    metadataDiv.style.right = '10px';
                    metadataDiv.style.backgroundColor = 'rgba(255, 255, 255, 0.9)';
                    metadataDiv.style.border = '1px solid #ccc';
                    metadataDiv.style.borderRadius = '8px';
                    metadataDiv.style.padding = '15px';
                    metadataDiv.style.zIndex = '9999';
                    metadataDiv.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.1)';
                    metadataDiv.style.maxHeight = '80vh';
                    metadataDiv.style.overflowY = 'auto';
                    metadataDiv.style.fontFamily = 'Arial, sans-serif';
                    metadataDiv.style.fontSize = '14px';
                    metadataDiv.style.animation = 'fadeIn 0.5s';
                    document.body.appendChild(metadataDiv);

                    const styleSheet = document.createElement('style');
                    styleSheet.type = 'text/css';
                    styleSheet.innerText = `
                        @keyframes fadeIn {
                            from { opacity: 0; }
                            to { opacity: 1; }
                        }
                    `;
                    document.head.appendChild(styleSheet);
                }

                const pageMetadata = await fetchMetadataFromPage();
                const combinedMetadata = { imageMetadata: response, pageMetadata: pageMetadata };
                let metadataHTML = `<h4 style="margin-top: 0; color: #333;">Current Image Metadata:</h4>`;
                for (const directory in response) {
                    metadataHTML += `<h5 style="margin-bottom: 5px; color: #555;">${directory}</h5>`;
                    const directoryMetadata = response[directory];
                    for (const tag in directoryMetadata) {
                        metadataHTML += `<p style="margin: 2px 0;"><strong>${tag}:</strong> ${directoryMetadata[tag]}</p>`;
                    }
                }
                metadataHTML += `<h4 style="margin-top: 20px; color: #333;">Current Page Metadata:</h4>`;
                for (const key in pageMetadata) {
                    metadataHTML += `<p style="margin: 2px 0;"><strong>${key}:</strong> ${pageMetadata[key]}</p>`;
                }

                // Add buttons
                metadataHTML += `<button id="viewHistory" style="margin-top: 10px; padding: 5px 10px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;">View History</button>`;
                metadataHTML += `<button id="closeMetadata" style="position: absolute; top: 5px; right: 10px; padding: 5px; background-color: #ccc; color: #333; border: none; border-radius: 4px; cursor: pointer;">Close</button>`;

                metadataDiv.innerHTML = metadataHTML;

                // Save metadata to localStorage
                var history = JSON.parse(localStorage.getItem('metadataHistory')) || [];
                history.push(combinedMetadata);
                localStorage.setItem('metadataHistory', JSON.stringify(history));
                const originalMetadataHTML = metadataHTML;
                document.getElementById('viewHistory').addEventListener('click', function () {
                    viewHistory(metadataDiv, originalMetadataHTML);
                });

                // Add event listener to the Close button
                document.getElementById('closeMetadata').addEventListener('click', function () {
                    if (metadataDiv) {
                        metadataDiv.remove();
                    }
                });
            }

        } else {
            alert('Error fetching metadata. Status Code: ' + xhr.status);
        }
    };
    xhr.onerror = function () {
        alert('Request failed. Please check your connection.');
    };
    xhr.send(formData);
}

function viewHistory(metadataDiv, originalMetadataHTML) {
    const history = JSON.parse(localStorage.getItem('metadataHistory')) || [];
    if (history.length === 0) {
        alert('No history available.');
        return;
    }

    // Store the original metadata HTML
    const currentMetadataHTML = metadataDiv.innerHTML;

    // Generate HTML for history
    let historyHTML = `<h4 style="margin-top: 0; color: #333;">Metadata History:</h4>`;
    history.forEach((metadata, index) => {
        historyHTML += `<h5 style="margin-bottom: 5px; color: #555;">Image ${index + 1}</h5>`;
        for (const directory in metadata.imageMetadata) {
            historyHTML += `<h5 style="margin-bottom: 5px; color: #555;">${directory}</h5>`;
            const directoryMetadata = metadata.imageMetadata[directory];
            for (const tag in directoryMetadata) {
                historyHTML += `<p style="margin: 2px 0;"><strong>${tag}:</strong> ${directoryMetadata[tag]}</p>`;
            }
        }
        historyHTML += `<h5 style="margin-bottom: 5px; color: #555;">Page Metadata</h5>`;
        for (const key in metadata.pageMetadata) {
            historyHTML += `<p style="margin: 2px 0;"><strong>${key}:</strong> ${metadata.pageMetadata[key]}</p>`;
        }
    });

    // Add close button
    historyHTML += `<button id="closeHistory" style="position: absolute; top: 5px; right: 10px; padding: 5px; background-color: #ccc; color: #333; border: none; border-radius: 4px; cursor: pointer;">Close</button>`;

    metadataDiv.innerHTML = historyHTML;

    // Add event listener to the Close button
    document.getElementById('closeHistory').addEventListener('click', function () {
        metadataDiv.innerHTML = currentMetadataHTML;
        document.getElementById('viewHistory').addEventListener('click', function () {
            viewHistory(metadataDiv, originalMetadataHTML);
        });
    });
}

async function fetchMetadataFromPage() {
    const url = window.location.href;
    const response = await fetch(url);
    const text = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(text, 'text/html');

    const metadata = {
        title: doc.querySelector('title')?.innerText || '',
        description: doc.querySelector('meta[name="description"]')?.getAttribute('content') || '',
        keywords: doc.querySelector('meta[name="keywords"]')?.getAttribute('content') || '',
        author: doc.querySelector('meta[name="author"]')?.getAttribute('content') || '',
        publishedDate: doc.querySelector('meta[name="published_date"]')?.getAttribute('content') || '',
        charset: doc.querySelector('meta[charset]')?.getAttribute('charset') || '',
        language: doc.querySelector('html')?.getAttribute('lang') || '',
        ogTitle: doc.querySelector('meta[property="og:title"]')?.getAttribute('content') || '',
        ogDescription: doc.querySelector('meta[property="og:description"]')?.getAttribute('content') || '',
        ogImage: doc.querySelector('meta[property="og:image"]')?.getAttribute('content') || '',
        twitterTitle: doc.querySelector('meta[name="twitter:title"]')?.getAttribute('content') || '',
        twitterDescription: doc.querySelector('meta[name="twitter:description"]')?.getAttribute('content') || '',
        twitterImage: doc.querySelector('meta[name="twitter:image"]')?.getAttribute('content') || '',
        canonicalUrl: doc.querySelector('link[rel="canonical"]')?.getAttribute('href') || ''
    };

    console.log(metadata);
    return metadata;
}

document.addEventListener('mouseover', function (event) {
    var target = event.target;
    if (target.tagName === 'IMG') {
        var imgUrl = target.src;
        scrapeImageMetadata(imgUrl);
    }
});













