import os
import sys
import subprocess
import base64
from pdf2image import convert_from_path
from flask import Flask,Response,request, jsonify, send_file,render_template,send_from_directory
render_template
from flask_cors import CORS


# Initialize Flask app
app = Flask(__name__)
CORS(app) 


def pptx_to_pdf(input_file, output_dir):
    command = ['libreoffice', '--headless', '--convert-to', 'pdf', input_file, '--outdir', output_dir]
    subprocess.run(command, check=True, capture_output=True, text=True)

def pdf_to_images(pdf_file, output_folder):
    """
    Convertit un fichier PDF en images (une image par slide) et les enregistre dans output_folder.
    """
    pages = convert_from_path(pdf_file)
    for i, page in enumerate(pages, start=1):
        image_path = os.path.join(output_folder, f"slide_{i}.png")
        page.save(image_path, 'PNG')
        print(f"Enregistré {image_path}")

def process_pptx(pptx_path, output_base_folder="output"):
    """
    Pour chaque fichier PPTX, crée un dossier dans 'output' portant le nom du fichier (sans extension),
    convertit le PPTX en PDF puis le PDF en images.
    """
    base_name = os.path.splitext(os.path.basename(pptx_path))[0]
    output_dir = os.path.join(output_base_folder, base_name)
    os.makedirs(output_dir, exist_ok=True)
    
    # Dossier temporaire pour stocker le PDF
    temp_pdf_dir = "temp_pdf"
    os.makedirs(temp_pdf_dir, exist_ok=True)
    
    # Conversion du PPTX en PDF
    print(f"Conversion de {pptx_path} en PDF...")
    pptx_to_pdf(pptx_path, temp_pdf_dir)
    
    pdf_file = os.path.join(temp_pdf_dir, f"{base_name}.pdf")
    if not os.path.exists(pdf_file):
        raise FileNotFoundError(f"La conversion en PDF a échoué : {pdf_file} est introuvable.")
    
    # Conversion du PDF en images slide par slide
    print(f"Conversion de {pdf_file} en images...")
    pdf_to_images(pdf_file, output_dir)
    print(f"Traitement de {pptx_path} terminé. Les images sont dans {output_dir}")

# Flask route to handle PPTX file uploads and processing
@app.route('/upload', methods=['POST'])
def upload_pptx():
    input_folder = "/input"
    output_folder = "/output"

    if not os.path.isdir(input_folder):
        return jsonify({"error": f"Le dossier d'entrée '{input_folder}' n'existe pas."}), 400

    pptx_file = request.files.get('file')
    if not pptx_file:
        return jsonify({"error": "Aucun fichier PPTX reçu."}), 400
    
    pptx_path = os.path.join(input_folder, pptx_file.filename)
    pptx_file.save(pptx_path)
    
    try:
        process_pptx(pptx_path, output_folder)
        return jsonify({"message": "Fichier PPTX traité avec succès!"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500
    



# IMAGE_FOLDER = "/output/slides"  

def encode_image_to_base64(image_path):
    """Encodes an image to base64."""
    with open(image_path, "rb") as image_file:
        return base64.b64encode(image_file.read()).decode("utf-8")

# @app.route('/liste', methods=['GET'])
# def f():
#     """Displays all available images in the folder as base64 encoded."""
#     if not os.path.exists(IMAGE_FOLDER):
#         return jsonify({"error": f"Le dossier {IMAGE_FOLDER} n'existe pas."}), 500

#     images = []
#     for filename in os.listdir(IMAGE_FOLDER):
#         if filename.endswith((".png", ".jpg", ".jpeg", ".gif")):
#             image_path = os.path.join(IMAGE_FOLDER, filename)
#             try:
#                 image_base64 = encode_image_to_base64(image_path)
#                 images.append(f"data:image/{filename.split('.')[-1]};base64,{image_base64}")
#             except Exception as e:
#                 return jsonify({"error": f"Erreur lors de l'encodage de l'image {filename}: {str(e)}"}), 500
    
#     return jsonify({"images": images}), 200


@app.route('/liste-images', methods=['GET'])
def list_images():
    """Affiche toutes les images disponibles dans un dossier donné sous forme encodée en base64."""
    folder_param = request.args.get('folder')  # Par exemple : /liste?folder=/output/9737
    if not folder_param:
        return jsonify({"error": "Le paramètre 'folder' est requis."}), 400

    if not os.path.exists(folder_param):
        return jsonify({"error": f"Le dossier {folder_param} n'existe pas."}), 500

    images = []
    for filename in os.listdir(folder_param):
        if filename.endswith((".png", ".jpg", ".jpeg", ".gif")):
            image_path = os.path.join(folder_param, filename)
            try:
                image_base64 = encode_image_to_base64(image_path)
                images.append(f"data:image/{filename.split('.')[-1]};base64,{image_base64}")
            except Exception as e:
                return jsonify({"error": f"Erreur lors de l'encodage de l'image {filename}: {str(e)}"}), 500
    
    if not images:
        return jsonify({"message": "Aucune image trouvée."}), 404
    
    return jsonify({"images": images}), 200


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)

