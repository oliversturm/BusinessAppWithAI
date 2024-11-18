import { useState } from "react";

const RuleEditor = ({ name, onChange, value, configureRule }) => {
  const [configureRuleActive, setConfigureRuleActive] = useState(false);
  const innerConfigureRule = (name, value) => {
    setConfigureRuleActive(true);
    configureRule(name, value).then(() => {
      setConfigureRuleActive(false);
    });
  };

  return (
    <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
      <textarea className="flex-grow" onChange={onChange(name)} value={value} />
      <button
        className="bg-red-300 rounded px-2 disabled:bg-gray-300"
        type="button"
        onClick={() => innerConfigureRule(name, value)}
        disabled={configureRuleActive.age}
      >
        Set Rule
      </button>
    </div>
  );
};

export default RuleEditor;
