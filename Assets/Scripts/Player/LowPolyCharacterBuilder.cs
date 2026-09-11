using UnityEngine;

namespace SeekingForJade.Player
{
    public static class LowPolyCharacterBuilder
    {
        public static GameObject BuildFirstPersonHands(Transform cameraTransform, Material skinMat, Material shirtMat, Material metalMat)
        {
            Transform existing = cameraTransform.Find("FPHands");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject fpHands = new GameObject("FPHands");
            fpHands.transform.SetParent(cameraTransform);
            fpHands.transform.localPosition = Vector3.zero;
            fpHands.transform.localRotation = Quaternion.identity;

            // --- Left Arm ---
            GameObject leftArmGroup = new GameObject("LeftArm");
            leftArmGroup.transform.SetParent(fpHands.transform);
            leftArmGroup.transform.localPosition = new Vector3(-0.32f, -0.32f, 0.45f);
            leftArmGroup.transform.localRotation = Quaternion.Euler(15f, 22f, -12f);

            // Sleeve Cuff
            GameObject lSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lSleeve.name = "LeftSleeve";
            lSleeve.transform.SetParent(leftArmGroup.transform);
            lSleeve.transform.localPosition = Vector3.zero;
            lSleeve.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            lSleeve.transform.localScale = new Vector3(0.11f, 0.14f, 0.11f);
            StripCollider(lSleeve);
            if (shirtMat != null) lSleeve.GetComponent<MeshRenderer>().sharedMaterial = shirtMat;

            // Forearm
            GameObject lForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lForearm.name = "LeftForearm";
            lForearm.transform.SetParent(leftArmGroup.transform);
            lForearm.transform.localPosition = new Vector3(0.02f, 0.04f, 0.18f);
            lForearm.transform.localRotation = Quaternion.Euler(85f, 5f, 0f);
            lForearm.transform.localScale = new Vector3(0.085f, 0.12f, 0.085f);
            StripCollider(lForearm);
            if (skinMat != null) lForearm.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Hand
            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lHand.name = "LeftHand";
            lHand.transform.SetParent(leftArmGroup.transform);
            lHand.transform.localPosition = new Vector3(0.04f, 0.05f, 0.32f);
            lHand.transform.localRotation = Quaternion.Euler(10f, 15f, -15f);
            lHand.transform.localScale = new Vector3(0.09f, 0.06f, 0.11f);
            StripCollider(lHand);
            if (skinMat != null) lHand.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // --- Right Arm (Flashlight Grip) ---
            GameObject rightArmGroup = new GameObject("RightArm");
            rightArmGroup.transform.SetParent(fpHands.transform);
            rightArmGroup.transform.localPosition = new Vector3(0.32f, -0.32f, 0.45f);
            rightArmGroup.transform.localRotation = Quaternion.Euler(12f, -18f, 10f);

            // Sleeve Cuff
            GameObject rSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rSleeve.name = "RightSleeve";
            rSleeve.transform.SetParent(rightArmGroup.transform);
            rSleeve.transform.localPosition = Vector3.zero;
            rSleeve.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            rSleeve.transform.localScale = new Vector3(0.11f, 0.14f, 0.11f);
            StripCollider(rSleeve);
            if (shirtMat != null) rSleeve.GetComponent<MeshRenderer>().sharedMaterial = shirtMat;

            // Forearm
            GameObject rForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rForearm.name = "RightForearm";
            rForearm.transform.SetParent(rightArmGroup.transform);
            rForearm.transform.localPosition = new Vector3(-0.02f, 0.04f, 0.18f);
            rForearm.transform.localRotation = Quaternion.Euler(85f, -5f, 0f);
            rForearm.transform.localScale = new Vector3(0.085f, 0.12f, 0.085f);
            StripCollider(rForearm);
            if (skinMat != null) rForearm.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Hand
            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rHand.name = "RightHand";
            rHand.transform.SetParent(rightArmGroup.transform);
            rHand.transform.localPosition = new Vector3(-0.04f, 0.05f, 0.32f);
            rHand.transform.localRotation = Quaternion.Euler(10f, -10f, 12f);
            rHand.transform.localScale = new Vector3(0.09f, 0.06f, 0.11f);
            StripCollider(rHand);
            if (skinMat != null) rHand.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Stylized Gemological Flashlight held in right hand
            GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torch.name = "GemFlashlightBody";
            torch.transform.SetParent(rHand.transform);
            torch.transform.localPosition = new Vector3(0f, 0.02f, 0.03f);
            torch.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            torch.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
            StripCollider(torch);
            if (metalMat != null) torch.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            // Flashlight Bezel
            GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bezel.name = "FlashlightBezel";
            bezel.transform.SetParent(torch.transform);
            bezel.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            bezel.transform.localRotation = Quaternion.identity;
            bezel.transform.localScale = new Vector3(1.35f, 0.22f, 1.35f);
            StripCollider(bezel);
            if (metalMat != null) bezel.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            return fpHands;
        }

        public static GameObject BuildThirdPersonAvatar(GameObject playerRoot, Material skinMat, Material shirtMat, Material pantsMat, Material leatherMat)
        {
            Transform existing = playerRoot.transform.Find("AvatarVisuals");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject avatar = new GameObject("AvatarVisuals");
            avatar.transform.SetParent(playerRoot.transform);
            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;

            // 1. Torso & Apron
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(avatar.transform);
            torso.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            torso.transform.localScale = new Vector3(0.52f, 0.62f, 0.32f);
            StripCollider(torso);
            if (shirtMat != null) torso.GetComponent<MeshRenderer>().sharedMaterial = shirtMat;

            // Leather Belt / Tool Pouch
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Belt";
            belt.transform.SetParent(avatar.transform);
            belt.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            belt.transform.localScale = new Vector3(0.54f, 0.10f, 0.34f);
            StripCollider(belt);
            if (leatherMat != null) belt.GetComponent<MeshRenderer>().sharedMaterial = leatherMat;

            // 2. Head & Cap
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(avatar.transform);
            head.transform.localPosition = new Vector3(0f, 1.48f, 0f);
            head.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
            StripCollider(head);
            if (skinMat != null) head.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Artisan Flat Cap
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "ArtisanCap";
            cap.transform.SetParent(head.transform);
            cap.transform.localPosition = new Vector3(0f, 0.52f, -0.02f);
            cap.transform.localScale = new Vector3(1.12f, 0.32f, 1.15f);
            StripCollider(cap);
            if (leatherMat != null) cap.GetComponent<MeshRenderer>().sharedMaterial = leatherMat;

            // Cap Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "CapVisor";
            visor.transform.SetParent(cap.transform);
            visor.transform.localPosition = new Vector3(0f, -0.2f, 0.6f);
            visor.transform.localScale = new Vector3(0.92f, 0.18f, 0.45f);
            StripCollider(visor);
            if (leatherMat != null) visor.GetComponent<MeshRenderer>().sharedMaterial = leatherMat;

            // 3. Legs & Boots
            float[] legX = new float[] { -0.15f, 0.15f };
            for (int i = 0; i < 2; i++)
            {
                // Leg
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(avatar.transform);
                leg.transform.localPosition = new Vector3(legX[i], 0.36f, 0f);
                leg.transform.localScale = new Vector3(0.20f, 0.54f, 0.22f);
                StripCollider(leg);
                if (pantsMat != null) leg.GetComponent<MeshRenderer>().sharedMaterial = pantsMat;

                // Boot
                GameObject boot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boot.name = $"Boot_{i}";
                boot.transform.SetParent(avatar.transform);
                boot.transform.localPosition = new Vector3(legX[i], 0.08f, 0.04f);
                boot.transform.localScale = new Vector3(0.22f, 0.16f, 0.30f);
                StripCollider(boot);
                if (leatherMat != null) boot.GetComponent<MeshRenderer>().sharedMaterial = leatherMat;
            }

            // 4. Arms
            float[] armX = new float[] { -0.34f, 0.34f };
            for (int i = 0; i < 2; i++)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = $"Arm_{i}";
                arm.transform.SetParent(avatar.transform);
                arm.transform.localPosition = new Vector3(armX[i], 0.94f, 0f);
                arm.transform.localScale = new Vector3(0.14f, 0.52f, 0.16f);
                StripCollider(arm);
                if (shirtMat != null) arm.GetComponent<MeshRenderer>().sharedMaterial = shirtMat;

                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hand.name = $"Hand_{i}";
                hand.transform.SetParent(arm.transform);
                hand.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                hand.transform.localScale = new Vector3(0.85f, 0.35f, 0.85f);
                StripCollider(hand);
                if (skinMat != null) hand.GetComponent<MeshRenderer>().sharedMaterial = skinMat;
            }

            return avatar;
        }

        public static GameObject BuildMerchantNPC(Transform parent, Vector3 localPosition, Quaternion localRotation, Material skinMat, Material silkMat, Material goldMat, Material hairMat, Material jadeMat)
        {
            Transform existing = parent.Find("MasterChenNPC");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject npc = new GameObject("MasterChenNPC");
            npc.transform.SetParent(parent);
            npc.transform.localPosition = localPosition;
            npc.transform.localRotation = localRotation;

            // Character Capsule Collider for player interaction raycasts
            CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.15f, 0f);
            col.height = 2.3f;
            col.radius = 0.50f;

            // 0. Red Silk Merchant Dais / Stand (elevates Master Chen above the counter)
            GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dais.name = "MerchantDais";
            dais.transform.SetParent(npc.transform, false);
            dais.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            dais.transform.localRotation = Quaternion.identity;
            dais.transform.localScale = new Vector3(1.6f, 0.20f, 1.2f);
            StripCollider(dais);
            if (silkMat != null) dais.GetComponent<MeshRenderer>().sharedMaterial = silkMat;

            // 1. Lower Robes / Skirt (Traditional dignified long merchant robes)
            GameObject lowerRobe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lowerRobe.name = "LowerRobe";
            lowerRobe.transform.SetParent(npc.transform, false);
            lowerRobe.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            lowerRobe.transform.localRotation = Quaternion.identity;
            lowerRobe.transform.localScale = new Vector3(0.55f, 0.55f, 0.45f);
            StripCollider(lowerRobe);
            if (silkMat != null) lowerRobe.GetComponent<MeshRenderer>().sharedMaterial = silkMat;

            // 2. Torso / Silk Vest
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(npc.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.30f, 0f);
            torso.transform.localRotation = Quaternion.identity;
            torso.transform.localScale = new Vector3(0.60f, 0.75f, 0.38f);
            StripCollider(torso);
            if (silkMat != null) torso.GetComponent<MeshRenderer>().sharedMaterial = silkMat;

            // Golden Embroidered Sash / Belt
            GameObject sash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sash.name = "GoldenSash";
            sash.transform.SetParent(torso.transform, false);
            sash.transform.localPosition = new Vector3(0f, -0.40f, 0f);
            sash.transform.localRotation = Quaternion.identity;
            sash.transform.localScale = new Vector3(1.08f, 0.20f, 1.10f);
            StripCollider(sash);
            if (goldMat != null) sash.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // Golden Lapel / Collar Trim
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collar.name = "CollarTrim";
            collar.transform.SetParent(torso.transform, false);
            collar.transform.localPosition = new Vector3(0f, 0.42f, 0.12f);
            collar.transform.localRotation = Quaternion.identity;
            collar.transform.localScale = new Vector3(0.42f, 0.25f, 0.90f);
            StripCollider(collar);
            if (goldMat != null) collar.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // 3. Head & Features
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(npc.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.88f, 0f);
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = new Vector3(0.38f, 0.40f, 0.38f);
            StripCollider(head);
            if (skinMat != null) head.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Merchant Skullcap / Hat (Rests proudly above the head cube)
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "MerchantHat";
            hat.transform.SetParent(head.transform, false);
            hat.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            hat.transform.localRotation = Quaternion.identity;
            hat.transform.localScale = new Vector3(1.18f, 0.35f, 1.18f);
            StripCollider(hat);
            if (hairMat != null) hat.GetComponent<MeshRenderer>().sharedMaterial = hairMat;

            // Jade Cabochon on Hat Center
            GameObject hatJade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hatJade.name = "HatJadeMedallion";
            hatJade.transform.SetParent(hat.transform, false);
            hatJade.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            hatJade.transform.localRotation = Quaternion.identity;
            hatJade.transform.localScale = new Vector3(0.28f, 0.28f, 0.22f);
            StripCollider(hatJade);
            if (jadeMat != null) hatJade.GetComponent<MeshRenderer>().sharedMaterial = jadeMat;

            // Expressive Dark Eyes
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eye.name = e < 0 ? "LeftEye" : "RightEye";
                eye.transform.SetParent(head.transform, false);
                eye.transform.localPosition = new Vector3(e * 0.20f, 0.15f, 0.53f);
                eye.transform.localRotation = Quaternion.identity;
                eye.transform.localScale = new Vector3(0.12f, 0.08f, 0.10f);
                StripCollider(eye);
                if (hairMat != null) eye.GetComponent<MeshRenderer>().sharedMaterial = hairMat;
            }

            // Mustache
            GameObject mustache = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mustache.name = "Mustache";
            mustache.transform.SetParent(head.transform, false);
            mustache.transform.localPosition = new Vector3(0f, -0.15f, 0.58f);
            mustache.transform.localRotation = Quaternion.identity;
            mustache.transform.localScale = new Vector3(0.75f, 0.16f, 0.25f);
            StripCollider(mustache);
            if (hairMat != null) mustache.GetComponent<MeshRenderer>().sharedMaterial = hairMat;

            // Goatee
            GameObject goatee = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goatee.name = "Goatee";
            goatee.transform.SetParent(head.transform, false);
            goatee.transform.localPosition = new Vector3(0f, -0.48f, 0.56f);
            goatee.transform.localRotation = Quaternion.identity;
            goatee.transform.localScale = new Vector3(0.28f, 0.38f, 0.25f);
            StripCollider(goatee);
            if (hairMat != null) goatee.GetComponent<MeshRenderer>().sharedMaterial = hairMat;

            // Gemologist Monocle / Loupe over right eye
            GameObject monocleRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            monocleRim.name = "LoupeRim";
            monocleRim.transform.SetParent(head.transform, false);
            monocleRim.transform.localPosition = new Vector3(0.20f, 0.15f, 0.60f);
            monocleRim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            monocleRim.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
            StripCollider(monocleRim);
            if (goldMat != null) monocleRim.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // Monocle Cord
            GameObject cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cord.name = "LoupeCord";
            cord.transform.SetParent(head.transform, false);
            cord.transform.localPosition = new Vector3(0.32f, -0.10f, 0.50f);
            cord.transform.localRotation = Quaternion.Euler(20f, 0f, 25f);
            cord.transform.localScale = new Vector3(0.03f, 0.35f, 0.03f);
            StripCollider(cord);
            if (goldMat != null) cord.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // 4. Arms & Welcoming Hands
            // Left Arm - resting near table counter
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(npc.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.40f, 1.25f, 0.22f);
            leftArm.transform.localRotation = Quaternion.Euler(35f, 15f, -10f);
            leftArm.transform.localScale = new Vector3(0.16f, 0.56f, 0.18f);
            StripCollider(leftArm);
            if (silkMat != null) leftArm.GetComponent<MeshRenderer>().sharedMaterial = silkMat;

            GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftHand.name = "LeftHand";
            leftHand.transform.SetParent(leftArm.transform, false);
            leftHand.transform.localPosition = new Vector3(0f, -0.55f, 0.12f);
            leftHand.transform.localRotation = Quaternion.identity;
            leftHand.transform.localScale = new Vector3(0.85f, 0.35f, 0.95f);
            StripCollider(leftHand);
            if (skinMat != null) leftHand.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Right Arm - gesturing toward the display stones
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(npc.transform, false);
            rightArm.transform.localPosition = new Vector3(0.40f, 1.25f, 0.26f);
            rightArm.transform.localRotation = Quaternion.Euler(45f, -25f, 12f);
            rightArm.transform.localScale = new Vector3(0.16f, 0.56f, 0.18f);
            StripCollider(rightArm);
            if (silkMat != null) rightArm.GetComponent<MeshRenderer>().sharedMaterial = silkMat;

            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightHand.name = "RightHand";
            rightHand.transform.SetParent(rightArm.transform, false);
            rightHand.transform.localPosition = new Vector3(0f, -0.55f, 0.15f);
            rightHand.transform.localRotation = Quaternion.identity;
            rightHand.transform.localScale = new Vector3(0.85f, 0.35f, 0.95f);
            StripCollider(rightHand);
            if (skinMat != null) rightHand.GetComponent<MeshRenderer>().sharedMaterial = skinMat;

            // Master Chen's personal inspection torch in right hand
            GameObject traderTorch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            traderTorch.name = "TraderInspectionLoupe";
            traderTorch.transform.SetParent(rightHand.transform, false);
            traderTorch.transform.localPosition = new Vector3(0f, -0.2f, 0.35f);
            traderTorch.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            traderTorch.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            StripCollider(traderTorch);
            if (goldMat != null) traderTorch.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            return npc;
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
        }
    }
}
