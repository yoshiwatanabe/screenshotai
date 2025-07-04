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
        this.modal = document.getElementById('detail-modal');
        this.modalTitle = document.getElementById('modal-title');
        this.modalImage = document.getElementById('modal-image');
        this.modalClose = document.getElementById('modal-close');
        this.analysisContent = document.getElementById('analysis-content');
    }

    attachEventListeners() {
        this.refreshBtn.addEventListener('click', () => this.loadImages());
        this.modalClose.addEventListener('click', () => this.closeModal());
        
        // Close modal when clicking outside
        this.modal.addEventListener('click', (e) => {
            if (e.target === this.modal) {
                this.closeModal();
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeModal();
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
        card.addEventListener('click', () => this.openModal(image));

        const createdDate = new Date(image.createdAt).toLocaleString();
        const fileSize = this.formatFileSize(image.fileSize);

        card.innerHTML = `
            <img src="${this.apiBase}/image/${image.fileName}" 
                 alt="${image.fileName}" 
                 class="image-preview"
                 loading="lazy">
            <div class="image-info">
                <div class="image-title">${image.fileName}</div>
                <div class="image-meta">
                    Created: ${createdDate}<br>
                    Size: ${fileSize}
                </div>
                <span class="analysis-status ${image.hasAnalysis ? 'analysis-available' : 'analysis-unavailable'}">
                    ${image.hasAnalysis ? 'Analyzed' : 'No Analysis'}
                </span>
            </div>
        `;

        return card;
    }

    async openModal(image) {
        this.currentImage = image;
        this.modalTitle.textContent = image.fileName;
        this.modalImage.src = `${this.apiBase}/image/${image.fileName}`;
        
        // Load analysis data if available
        if (image.hasAnalysis) {
            try {
                const response = await fetch(`${this.apiBase}/analysis/${image.fileName}`);
                if (response.ok) {
                    const analysisData = await response.json();
                    this.renderAnalysis(analysisData);
                } else {
                    this.analysisContent.innerHTML = '<p>Failed to load analysis data</p>';
                }
            } catch (error) {
                console.error('Error loading analysis:', error);
                this.analysisContent.innerHTML = '<p>Error loading analysis data</p>';
            }
        } else {
            this.analysisContent.innerHTML = '<p>No analysis available for this image</p>';
        }

        this.modal.style.display = 'block';
        document.body.style.overflow = 'hidden';
    }

    closeModal() {
        this.modal.style.display = 'none';
        document.body.style.overflow = 'auto';
        this.currentImage = null;
    }

    renderAnalysis(data) {
        if (!data) {
            this.analysisContent.innerHTML = '<p>No analysis data available</p>';
            return;
        }

        let html = '';

        // Handle different types of Azure Vision responses
        if (data.categories && data.categories.length > 0) {
            html += this.renderSection('Categories', data.categories.map(cat => 
                `<div class="analysis-item">
                    <h4>${cat.name}</h4>
                    <span class="confidence">Confidence: ${(cat.score * 100).toFixed(1)}%</span>
                </div>`
            ).join(''));
        }

        if (data.tags && data.tags.length > 0) {
            html += this.renderSection('Tags', data.tags.map(tag => 
                `<div class="analysis-item">
                    <h4>${tag.name}</h4>
                    <span class="confidence">Confidence: ${(tag.confidence * 100).toFixed(1)}%</span>
                </div>`
            ).join(''));
        }

        if (data.description && data.description.captions && data.description.captions.length > 0) {
            html += this.renderSection('Description', data.description.captions.map(caption => 
                `<div class="analysis-item">
                    <h4>${caption.text}</h4>
                    <span class="confidence">Confidence: ${(caption.confidence * 100).toFixed(1)}%</span>
                </div>`
            ).join(''));
        }

        if (data.objects && data.objects.length > 0) {
            html += this.renderSection('Objects', data.objects.map(obj => 
                `<div class="analysis-item">
                    <h4>${obj.object}</h4>
                    <span class="confidence">Confidence: ${(obj.confidence * 100).toFixed(1)}%</span>
                    <div>Rectangle: ${JSON.stringify(obj.rectangle)}</div>
                </div>`
            ).join(''));
        }

        if (data.faces && data.faces.length > 0) {
            html += this.renderSection('Faces', data.faces.map((face, index) => 
                `<div class="analysis-item">
                    <h4>Face ${index + 1}</h4>
                    <div>Age: ${face.age || 'Unknown'}</div>
                    <div>Gender: ${face.gender || 'Unknown'}</div>
                    <div>Rectangle: ${JSON.stringify(face.faceRectangle)}</div>
                </div>`
            ).join(''));
        }

        if (data.color) {
            html += this.renderSection('Color Analysis', 
                `<div class="analysis-item">
                    <h4>Dominant Colors</h4>
                    <div>Background: ${data.color.dominantColorBackground}</div>
                    <div>Foreground: ${data.color.dominantColorForeground}</div>
                    <div>Accent: ${data.color.accentColor}</div>
                </div>`
            );
        }

        // Always show raw JSON for debugging
        html += `
            <h3 style="margin-top: 20px;">Raw JSON Data</h3>
            <div class="json-view">${JSON.stringify(data, null, 2)}</div>
        `;

        this.analysisContent.innerHTML = html;
    }

    renderSection(title, content) {
        return `
            <h3 style="margin-top: 20px; margin-bottom: 10px;">${title}</h3>
            ${content}
        `;
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

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}

// Initialize the application when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ScreenshotViewer();
});