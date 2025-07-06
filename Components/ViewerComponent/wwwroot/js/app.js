class ScreenshotViewer {
    constructor() {
        this.apiBase = '/api';
        this.images = [];
        this.currentImage = null;
        
        this.initializeElements();
        this.attachEventListeners();
        this.loadImages();
        this.updateStatus();
        
        // Auto-refresh every 30 seconds
        setInterval(() => this.loadImages(), 30000);
    }

    initializeElements() {
        this.statusInfo = document.getElementById('status-info');
        this.refreshBtn = document.getElementById('refresh-btn');
        this.loading = document.getElementById('loading');
        this.noImages = document.getElementById('no-images');
        this.imageGrid = document.getElementById('image-grid');
        this.imageModal = document.getElementById('image-modal');
        this.modalImage = document.getElementById('modal-image');
        this.modalClose = document.getElementById('image-modal-close');
    }

    attachEventListeners() {
        this.refreshBtn.addEventListener('click', () => this.loadImages());
        
        // Image modal event listeners
        this.modalClose.addEventListener('click', () => this.closeImageModal());
        this.imageModal.addEventListener('click', (e) => {
            if (e.target === this.imageModal) {
                this.closeImageModal();
            }
        });
        
        // Keyboard shortcut to close modal
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeImageModal();
            }
        });
    }

    async loadImages() {
        try {
            this.showLoading(true);
            
            const response = await fetch(`${this.apiBase}/files`);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            this.images = await response.json();
            this.renderImages();
            
        } catch (error) {
            console.error('Error loading images:', error);
            this.showError('Failed to load images. Make sure the service is running.');
        } finally {
            this.showLoading(false);
        }
    }

    async updateStatus() {
        try {
            const response = await fetch(`${this.apiBase}/status`);
            if (!response.ok) return;
            
            const status = await response.json();
            this.statusInfo.textContent = 
                `${status.totalImages} images, ${status.imagesWithAnalysis} analyzed`;
                
        } catch (error) {
            console.error('Error updating status:', error);
            this.statusInfo.textContent = 'Status unavailable';
        }
    }

    renderImages() {
        if (this.images.length === 0) {
            this.imageGrid.style.display = 'none';
            this.noImages.style.display = 'block';
            return;
        }

        this.noImages.style.display = 'none';
        this.imageGrid.style.display = 'grid';
        this.imageGrid.innerHTML = '';

        this.images.forEach(image => {
            const card = this.createImageCard(image);
            this.imageGrid.appendChild(card);
        });

        this.updateStatus();
    }

    createImageCard(image) {
        const card = document.createElement('div');
        card.className = 'image-card';
        
        const createdDate = new Date(image.createdAt).toLocaleString();
        const fileSize = this.formatFileSize(image.fileSize);

        const safeId = image.fileName.replace(/[^a-zA-Z0-9]/g, '_');
        
        card.innerHTML = `
            <img src="${this.apiBase}/image/${image.fileName}" 
                 alt="${image.fileName}" 
                 class="image-preview"
                 loading="lazy"
                 style="cursor: pointer;"
                 onclick="window.screenshotViewer.openImageModal('${image.fileName}')">
            <div class="image-info">
                <div class="image-title">${image.fileName}</div>
                <div class="image-meta">
                    <span>Created: ${createdDate}</span>
                    <span>Size: ${fileSize}</span>
                    <span class="analysis-status ${image.hasAnalysis ? 'analysis-available' : 'analysis-unavailable'}">
                        ${image.hasAnalysis ? 'Analyzed' : 'No Analysis'}
                    </span>
                </div>
                <div id="parsed-${safeId}">
                    ${image.hasAnalysis ? '<div>Loading parsed analysis...</div>' : ''}
                </div>
            </div>
            <div class="json-section">
                <button class="json-toggle-btn" onclick="toggleJsonForImage('${safeId}')" title="Show/Hide Raw JSON">
                    📄 JSON
                </button>
                <div class="analysis-display" id="json-${safeId}" style="display: none;">
                    ${image.hasAnalysis ? 'Loading JSON...' : 'No analysis data'}
                </div>
            </div>
        `;

        // Load both parsed and JSON analysis if available
        if (image.hasAnalysis) {
            this.loadCompleteAnalysis(image);
        }

        return card;
    }

    async loadCompleteAnalysis(image) {
        try {
            const response = await fetch(`${this.apiBase}/analysis/${image.fileName}`);
            if (response.ok) {
                const analysisData = await response.json();
                console.log('Raw API response for', image.fileName, ':', analysisData);
                
                // Check if we got a pre-parsed string or raw JSON
                if (typeof analysisData === 'string') {
                    // It's already a parsed string - show it directly
                    this.renderPreParsedAnalysis(image, analysisData);
                    this.renderJsonAnalysis(image, { 
                        dataType: "Pre-parsed analysis string",
                        originalString: analysisData,
                        note: "This data was already processed by the backend"
                    });
                } else {
                    // It's raw JSON - parse it ourselves
                    this.renderParsedAnalysis(image, analysisData);
                    this.renderJsonAnalysis(image, analysisData);
                }
            }
        } catch (error) {
            console.error('Error loading analysis:', error);
        }
    }

    renderParsedAnalysis(image, data) {
        const parsedId = `parsed-${image.fileName.replace(/[^a-zA-Z0-9]/g, '_')}`;
        const parsedElement = document.getElementById(parsedId);
        if (!parsedElement) {
            console.error(`Element not found: ${parsedId}`);
            return;
        }

        console.log('Analysis data for parsing:', data);

        // Create parsed analysis string in your requested format
        let parsedParts = [];

        // Tags with confidence
        if (data.tags && Array.isArray(data.tags) && data.tags.length > 0) {
            const tagStr = data.tags
                .slice(0, 5)  // Top 5 tags
                .map(tag => `${tag.name}(${(tag.confidence || tag.score || 0).toFixed(1)})`)
                .join(', ');
            parsedParts.push(`Tags: ${tagStr}`);
        }

        // Text/Description
        if (data.description && data.description.captions && data.description.captions.length > 0) {
            const text = data.description.captions[0].text;
            parsedParts.push(`Text: ${text}`);
        }

        // Alternative text fields
        if (data.text && typeof data.text === 'string') {
            parsedParts.push(`Text: ${data.text}`);
        }

        // People count
        if (data.faces && Array.isArray(data.faces) && data.faces.length > 0) {
            parsedParts.push(`People: ${data.faces.length} person${data.faces.length > 1 ? 's' : ''} detected`);
        }

        // Objects
        if (data.objects && Array.isArray(data.objects) && data.objects.length > 0) {
            const objectStr = data.objects
                .slice(0, 3)  // Top 3 objects
                .map(obj => `${obj.object}(${(obj.confidence || obj.score || 0).toFixed(1)})`)
                .join(', ');
            parsedParts.push(`Objects: ${objectStr}`);
        }

        // Categories (fallback)
        if (parsedParts.length === 0 && data.categories && Array.isArray(data.categories) && data.categories.length > 0) {
            const catStr = data.categories
                .slice(0, 3)
                .map(cat => `${cat.name}(${(cat.score || cat.confidence || 0).toFixed(1)})`)
                .join(', ');
            parsedParts.push(`Categories: ${catStr}`);
        }

        console.log('Parsed parts:', parsedParts);

        if (parsedParts.length > 0) {
            const fullString = parsedParts.join('<br/>');
            parsedElement.innerHTML = `
                <div class="parsed-analysis" onclick="copyToClipboard(this)" title="Click to copy">
                    ${fullString}
                </div>
            `;
        } else {
            parsedElement.innerHTML = '<div>No analysis data could be parsed</div>';
            console.warn('No parseable data found in:', data);
        }
    }

    renderPreParsedAnalysis(image, parsedString) {
        const parsedId = `parsed-${image.fileName.replace(/[^a-zA-Z0-9]/g, '_')}`;
        const parsedElement = document.getElementById(parsedId);
        if (!parsedElement) {
            console.error(`Element not found: ${parsedId}`);
            return;
        }

        console.log('Got pre-parsed string:', parsedString);

        // Split by pipe and create separate divs for each line
        const lines = parsedString.split(' | ');
        const divElements = lines.map(line => `<div class="analysis-line">${line}</div>`).join('');
        
        parsedElement.innerHTML = `
            <div class="parsed-analysis">
                ${divElements}
            </div>
        `;
    }

    renderJsonAnalysis(image, data) {
        const jsonId = `json-${image.fileName.replace(/[^a-zA-Z0-9]/g, '_')}`;
        const jsonElement = document.getElementById(jsonId);
        if (!jsonElement) return;

        // Simple JSON display for now
        jsonElement.innerHTML = JSON.stringify(data, null, 2);
    }

    formatDataAsKeyValue(data, prefix = '') {
        // Safety check - make sure this is actually an object
        if (typeof data !== 'object' || data === null || Array.isArray(data) || typeof data === 'string') {
            console.error('formatDataAsKeyValue called with invalid data type:', typeof data, data);
            return JSON.stringify(data, null, 2);
        }

        let result = [];
        
        for (const [key, value] of Object.entries(data)) {
            const fullKey = prefix ? `${prefix}.${key}` : key;
            
            if (value === null || value === undefined) {
                result.push(`${fullKey}: null`);
            } else if (typeof value === 'object' && !Array.isArray(value)) {
                // Nested object - recurse but limit depth
                if (prefix.split('.').length < 2) {
                    result.push(`<span style="color: #2c3e50; font-weight: bold;">${fullKey}:</span>`);
                    result.push(this.formatDataAsKeyValue(value, fullKey));
                } else {
                    result.push(`${fullKey}: ${JSON.stringify(value)}`);
                }
            } else if (Array.isArray(value)) {
                if (value.length === 0) {
                    result.push(`${fullKey}: []`);
                } else if (typeof value[0] === 'object') {
                    // Array of objects
                    result.push(`<span style="color: #2c3e50; font-weight: bold;">${fullKey} (${value.length} items):</span>`);
                    value.forEach((item, index) => {
                        result.push(`  [${index}] ${this.formatObjectInline(item)}`);
                    });
                } else {
                    // Array of primitives
                    result.push(`${fullKey}: [${value.join(', ')}]`);
                }
            } else {
                // Primitive value
                const formattedValue = typeof value === 'string' ? `"${value}"` : value;
                result.push(`${fullKey}: ${formattedValue}`);
            }
        }
        
        return result.join('<br/>');
    }

    formatObjectInline(obj) {
        const pairs = [];
        for (const [key, value] of Object.entries(obj)) {
            if (typeof value === 'object' && value !== null) {
                pairs.push(`${key}: ${JSON.stringify(value)}`);
            } else {
                const formattedValue = typeof value === 'string' ? `"${value}"` : value;
                pairs.push(`${key}: ${formattedValue}`);
            }
        }
        return pairs.join(' | ');
    }


    showLoading(show) {
        this.loading.style.display = show ? 'block' : 'none';
    }

    showError(message) {
        this.statusInfo.textContent = message;
        this.statusInfo.style.color = '#e74c3c';
        setTimeout(() => {
            this.statusInfo.style.color = '';
            this.updateStatus();
        }, 5000);
    }

    openImageModal(fileName) {
        this.modalImage.src = `${this.apiBase}/image/${fileName}`;
        this.imageModal.style.display = 'block';
        document.body.style.overflow = 'hidden';
    }

    closeImageModal() {
        this.imageModal.style.display = 'none';
        document.body.style.overflow = 'auto';
        this.modalImage.src = '';
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}

// Global functions for UI interactions
function toggleJsonForImage(imageId) {
    const jsonElement = document.getElementById(`json-${imageId}`);
    const button = event.target;
    
    if (jsonElement) {
        const isHidden = jsonElement.style.display === 'none';
        jsonElement.style.display = isHidden ? 'block' : 'none';
        button.textContent = isHidden ? '📄 Hide JSON' : '📄 JSON';
    }
}

// Initialize the application when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.screenshotViewer = new ScreenshotViewer();
});