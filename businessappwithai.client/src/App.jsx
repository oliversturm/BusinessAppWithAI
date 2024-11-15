import FormikInputForm from "@/FormikInputForm.jsx";
import InputForm from "@/InputForm.jsx";
import AIInputForm from "@/AIInputForm.jsx";

function App() {
  return (
    <div className="container mx-auto max-w-2xl">
      <h1 className="text-4xl font-bold mb-6">
        Business App <span className="line-through">with AI</span>
      </h1>
      <InputForm />
      <FormikInputForm />
      <AIInputForm />
    </div>
  );
}

export default App;
