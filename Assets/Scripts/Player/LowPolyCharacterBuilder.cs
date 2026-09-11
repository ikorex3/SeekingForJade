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
